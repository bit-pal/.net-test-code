using Microsoft.Extensions.Logging;
using SearchOrchestrator.Core.Contracts;
using SearchOrchestrator.Core.Models;
using SearchOrchestrator.Core.Services;

namespace SearchOrchestrator.Core.Services;

/// <summary>
/// Реализация оркестратора: постановка задач, идемпотентность, обработка ошибок внешнего сервиса.
/// Фоновое обновление статусов можно добавить отдельным hosted service, здесь — синхронная постановка.
/// </summary>
public class SearchOrchestratorService : ISearchOrchestratorService
{
    private readonly IIndexingTaskStore _store;
    private readonly ISearchEngineClient _searchEngine;
    private readonly ILogger<SearchOrchestratorService> _logger;

    public SearchOrchestratorService(
        IIndexingTaskStore store,
        ISearchEngineClient searchEngine,
        ILogger<SearchOrchestratorService> logger)
    {
        _store = store;
        _searchEngine = searchEngine;
        _logger = logger;
    }

    public async Task<StartIndexingOutcome> StartIndexingAsync(
        IReadOnlyList<string> sourceIds,
        string? correlationId = null,
        CancellationToken cancellationToken = default)
    {
        correlationId ??= Guid.NewGuid().ToString("N");
        if (sourceIds.Count == 0)
        {
            _logger.LogWarning("StartIndexing called with empty sourceIds, CorrelationId={CorrelationId}", correlationId);
            return new StartIndexingOutcome
            {
                Accepted = false,
                Task = CreateStubTask(correlationId, sourceIds),
                RejectionReason = "SourceIds cannot be empty."
            };
        }

        var sources = await _store.GetSourcesByIdsAsync(sourceIds, cancellationToken).ConfigureAwait(false);
        var missing = sourceIds.Except(sources.Select(s => s.Id)).ToList();
        if (missing.Count > 0)
        {
            _logger.LogWarning("StartIndexing: unknown source ids {Missing}, CorrelationId={CorrelationId}", missing, correlationId);
            return new StartIndexingOutcome
            {
                Accepted = false,
                Task = CreateStubTask(correlationId, sourceIds),
                RejectionReason = $"Unknown source ids: {string.Join(", ", missing)}."
            };
        }

        var sortedIds = sourceIds.OrderBy(x => x).ToArray();
        var existingRunning = await _store.FindRunningTaskBySourceIdsAsync(sortedIds, cancellationToken).ConfigureAwait(false);
        if (existingRunning != null)
        {
            _logger.LogInformation(
                "StartIndexing idempotent: returning existing running task TaskId={TaskId}, CorrelationId={CorrelationId}",
                existingRunning.Id, correlationId);
            return new StartIndexingOutcome
            {
                Accepted = true,
                Task = existingRunning,
                IsExistingRunningTask = true
            };
        }

        var taskId = Guid.NewGuid().ToString("N");
        var externalIds = sources.Select(s => s.ExternalId).ToArray();
        var task = new IndexingTask
        {
            Id = taskId,
            CorrelationId = correlationId,
            SourceIds = sortedIds,
            Status = IndexingTaskStatus.Pending,
            CreatedAtUtc = DateTime.UtcNow
        };
        await _store.AddTaskAsync(task, cancellationToken).ConfigureAwait(false);

        _logger.LogInformation(
            "StartIndexing: created task TaskId={TaskId}, CorrelationId={CorrelationId}, SourceCount={Count}",
            taskId, correlationId, sourceIds.Count);

        task.Status = IndexingTaskStatus.Running;
        task.StartedAtUtc = DateTime.UtcNow;
        var submission = await _searchEngine.StartIndexingAsync(externalIds, cancellationToken).ConfigureAwait(false);

        if (submission.IsTimeout)
        {
            task.ErrorMessage = "Timeout while submitting to search engine.";
            task.Status = IndexingTaskStatus.Failed;
            task.CompletedAtUtc = DateTime.UtcNow;
            await _store.UpdateTaskAsync(task, cancellationToken).ConfigureAwait(false);
            _logger.LogWarning("StartIndexing: search engine timeout, TaskId={TaskId}", taskId);
            return new StartIndexingOutcome { Accepted = true, Task = task };
        }

        if (!submission.Success)
        {
            task.ErrorMessage = submission.ErrorMessage ?? "Search engine rejected the request.";
            task.Status = IndexingTaskStatus.Failed;
            task.CompletedAtUtc = DateTime.UtcNow;
            await _store.UpdateTaskAsync(task, cancellationToken).ConfigureAwait(false);
            _logger.LogWarning("StartIndexing: search engine error {Error}, TaskId={TaskId}", task.ErrorMessage, taskId);
            return new StartIndexingOutcome { Accepted = true, Task = task };
        }

        task.ExternalTaskId = submission.ExternalTaskId;
        if (submission.IsPartialSuccess)
        {
            task.Status = IndexingTaskStatus.PartiallyCompleted;
            task.Details = submission.PartialSuccessDetails;
            task.CompletedAtUtc = DateTime.UtcNow;
        }
        await _store.UpdateTaskAsync(task, cancellationToken).ConfigureAwait(false);
        _logger.LogInformation("StartIndexing: task submitted to search engine, TaskId={TaskId}, ExternalTaskId={ExternalId}", taskId, task.ExternalTaskId);
        return new StartIndexingOutcome { Accepted = true, Task = task };
    }

    public async Task<IndexingTask?> GetTaskStatusAsync(string taskIdOrCorrelationId, CancellationToken cancellationToken = default)
    {
        var byId = await _store.GetTaskByIdAsync(taskIdOrCorrelationId, cancellationToken).ConfigureAwait(false);
        if (byId != null) return byId;
        return await _store.GetTaskByCorrelationIdAsync(taskIdOrCorrelationId, cancellationToken).ConfigureAwait(false);
    }

    public async Task<CancelTaskResult> CancelTaskAsync(string taskId, CancellationToken cancellationToken = default)
    {
        var task = await _store.GetTaskByIdAsync(taskId, cancellationToken).ConfigureAwait(false);
        if (task == null)
            return new CancelTaskResult { Success = false, ErrorMessage = "Task not found." };

        if (task.Status != IndexingTaskStatus.Pending && task.Status != IndexingTaskStatus.Running)
            return new CancelTaskResult { Success = false, ErrorMessage = $"Task is already in terminal state: {task.Status}." };

        if (!string.IsNullOrEmpty(task.ExternalTaskId))
        {
            var cancelResult = await _searchEngine.CancelIndexingAsync(task.ExternalTaskId, cancellationToken).ConfigureAwait(false);
            if (cancelResult.IsTimeout)
                _logger.LogWarning("CancelTask: timeout calling search engine, TaskId={TaskId}", taskId);
            else if (!cancelResult.Success)
                _logger.LogWarning("CancelTask: search engine returned error {Error}, TaskId={TaskId}", cancelResult.ErrorMessage, taskId);
        }

        task.Status = IndexingTaskStatus.Cancelled;
        task.CompletedAtUtc = DateTime.UtcNow;
        await _store.UpdateTaskAsync(task, cancellationToken).ConfigureAwait(false);
        _logger.LogInformation("CancelTask: task cancelled, TaskId={TaskId}", taskId);
        return new CancelTaskResult { Success = true };
    }

    public async Task<SearchResult> SearchAsync(
        string query,
        int maxResults = 100,
        string? correlationId = null,
        CancellationToken cancellationToken = default)
    {
        correlationId ??= Guid.NewGuid().ToString("N");
        _logger.LogInformation("Search: Query={Query}, CorrelationId={CorrelationId}", query, correlationId);
        var result = await _searchEngine.SearchAsync(query, maxResults, cancellationToken).ConfigureAwait(false);
        return new SearchResult
        {
            Query = result.Query,
            Items = result.Items,
            TotalCount = result.TotalCount,
            CorrelationId = correlationId
        };
    }

    public async Task<string> RegisterSourceAsync(string externalId, string? displayName, CancellationToken cancellationToken = default)
    {
        var id = Guid.NewGuid().ToString("N");
        var source = new IndexingSource
        {
            Id = id,
            ExternalId = externalId,
            DisplayName = displayName,
            CreatedAtUtc = DateTime.UtcNow
        };
        await _store.AddOrUpdateSourceAsync(source, cancellationToken).ConfigureAwait(false);
        _logger.LogInformation("RegisterSource: Id={Id}, ExternalId={ExternalId}", id, externalId);
        return id;
    }

    private static IndexingTask CreateStubTask(string correlationId, IReadOnlyList<string> sourceIds)
    {
        return new IndexingTask
        {
            Id = Guid.NewGuid().ToString("N"),
            CorrelationId = correlationId,
            SourceIds = sourceIds,
            Status = IndexingTaskStatus.Failed,
            CreatedAtUtc = DateTime.UtcNow,
            CompletedAtUtc = DateTime.UtcNow
        };
    }
}

using System.Collections.Concurrent;
using SearchOrchestrator.Core.Contracts;
using SearchOrchestrator.Core.Models;

namespace SearchOrchestrator.Infrastructure;

/// <summary>
/// In-memory хранилище задач и источников (для каркаса и тестов).
/// В проде можно заменить на персистентное хранилище.
/// </summary>
public class InMemoryIndexingTaskStore : IIndexingTaskStore
{
    private readonly ConcurrentDictionary<string, IndexingTask> _tasks = new();
    private readonly ConcurrentDictionary<string, IndexingSource> _sources = new();
    private readonly ConcurrentDictionary<string, string> _correlationToTaskId = new();

    public Task<IndexingTask?> GetTaskByIdAsync(string taskId, CancellationToken cancellationToken = default)
    {
        _tasks.TryGetValue(taskId, out var t);
        return Task.FromResult(t);
    }

    public Task<IndexingTask?> GetTaskByCorrelationIdAsync(string correlationId, CancellationToken cancellationToken = default)
    {
        if (_correlationToTaskId.TryGetValue(correlationId, out var taskId) && _tasks.TryGetValue(taskId, out var t))
            return Task.FromResult<IndexingTask?>(t);
        var byCorrelation = _tasks.Values.FirstOrDefault(x => x.CorrelationId == correlationId);
        return Task.FromResult(byCorrelation);
    }

    public Task<IndexingTask?> FindRunningTaskBySourceIdsAsync(IReadOnlyList<string> sourceIds, CancellationToken cancellationToken = default)
    {
        var key = string.Join("|", sourceIds.OrderBy(x => x));
        var running = _tasks.Values.FirstOrDefault(t =>
            (t.Status == IndexingTaskStatus.Pending || t.Status == IndexingTaskStatus.Running) &&
            string.Join("|", t.SourceIds.OrderBy(x => x)) == key);
        return Task.FromResult(running);
    }

    public Task<string> AddTaskAsync(IndexingTask task, CancellationToken cancellationToken = default)
    {
        _tasks[task.Id] = task;
        _correlationToTaskId[task.CorrelationId] = task.Id;
        return Task.FromResult(task.Id);
    }

    public Task UpdateTaskAsync(IndexingTask task, CancellationToken cancellationToken = default)
    {
        _tasks[task.Id] = task;
        return Task.CompletedTask;
    }

    public Task<IndexingSource?> GetSourceByIdAsync(string sourceId, CancellationToken cancellationToken = default)
    {
        _sources.TryGetValue(sourceId, out var s);
        return Task.FromResult(s);
    }

    public Task<IReadOnlyList<IndexingSource>> GetSourcesByIdsAsync(IReadOnlyList<string> sourceIds, CancellationToken cancellationToken = default)
    {
        var list = sourceIds.Select(id => _sources.TryGetValue(id, out var s) ? s : null).Where(s => s != null).Cast<IndexingSource>().ToList();
        return Task.FromResult<IReadOnlyList<IndexingSource>>(list);
    }

    public Task<string> AddOrUpdateSourceAsync(IndexingSource source, CancellationToken cancellationToken = default)
    {
        _sources[source.Id] = source;
        return Task.FromResult(source.Id);
    }
}

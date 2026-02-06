using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SearchOrchestrator.Core.Contracts;
using SearchOrchestrator.Core.Models;

namespace SearchOrchestrator.Infrastructure;

/// <summary>
/// Заглушка внешнего сервиса поиска. Имитирует успех, ошибку, таймаут и частичный успех
/// на основе конфигурации (для демонстрации оркестрационной логики).
/// </summary>
public class MockSearchEngineClient : ISearchEngineClient
{
    private readonly MockSearchEngineOptions _options;
    private readonly ILogger<MockSearchEngineClient> _logger;
    private readonly Random _rnd = new();

    public MockSearchEngineClient(IOptions<MockSearchEngineOptions> options, ILogger<MockSearchEngineClient> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public async Task<IndexingSubmissionResult> StartIndexingAsync(
        IReadOnlyList<string> sourceExternalIds,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Mock: StartIndexing called for {Count} sources", sourceExternalIds.Count);

        if (_options.SimulateTimeout)
        {
            await Task.Delay(_options.TimeoutDelayMs, cancellationToken).ConfigureAwait(false);
            return new IndexingSubmissionResult { Success = false, IsTimeout = true, ErrorMessage = "Request timeout." };
        }

        if (_options.SimulateFailure)
        {
            return new IndexingSubmissionResult
            {
                Success = false,
                ErrorMessage = _options.FailureMessage ?? "Simulated failure."
            };
        }

        if (_options.SimulatePartialSuccess)
        {
            var externalId = "ext-" + Guid.NewGuid().ToString("N")[..8];
            return new IndexingSubmissionResult
            {
                Success = true,
                ExternalTaskId = externalId,
                IsPartialSuccess = true,
                PartialSuccessDetails = "Source " + sourceExternalIds[0] + " failed; others indexed."
            };
        }

        await Task.Delay(_options.IndexingDelayMs, cancellationToken).ConfigureAwait(false);
        var taskId = "ext-" + Guid.NewGuid().ToString("N")[..8];
        return new IndexingSubmissionResult
        {
            Success = true,
            ExternalTaskId = taskId
        };
    }

    public Task<ExternalTaskStatusResult> GetIndexingStatusAsync(string externalTaskId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Mock: GetIndexingStatus for {ExternalTaskId}", externalTaskId);
        return Task.FromResult(new ExternalTaskStatusResult
        {
            Success = true,
            Status = IndexingTaskStatus.Completed
        });
    }

    public Task<CancelTaskResult> CancelIndexingAsync(string externalTaskId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Mock: CancelIndexing for {ExternalTaskId}", externalTaskId);
        return Task.FromResult(new CancelTaskResult { Success = true });
    }

    public async Task<SearchResult> SearchAsync(string query, int maxResults = 100, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Mock: Search query={Query}", query);
        if (_options.SimulateSearchTimeout)
        {
            await Task.Delay(_options.TimeoutDelayMs, cancellationToken).ConfigureAwait(false);
            throw new TimeoutException("Search request timeout.");
        }
        if (_options.SimulateSearchFailure)
        {
            throw new InvalidOperationException(_options.FailureMessage ?? "Search failed.");
        }

        var items = Enumerable.Range(1, Math.Min(3, maxResults))
            .Select(i => new SearchResultItem
            {
                Id = "doc-" + i,
                Title = "Document " + i,
                Snippet = "Snippet for \"" + query + "\" in doc " + i,
                SourceId = "src-1"
            })
            .ToList();
        return new SearchResult
        {
            Query = query,
            Items = items,
            TotalCount = items.Count
        };
    }
}

/// <summary>
/// Настройки поведения mock (для тестов и демо).
/// </summary>
public class MockSearchEngineOptions
{
    public const string SectionName = "MockSearchEngine";
    public int IndexingDelayMs { get; set; } = 50;
    public int TimeoutDelayMs { get; set; } = 5000;
    public bool SimulateTimeout { get; set; }
    public bool SimulateFailure { get; set; }
    public bool SimulatePartialSuccess { get; set; }
    public bool SimulateSearchTimeout { get; set; }
    public bool SimulateSearchFailure { get; set; }
    public string? FailureMessage { get; set; }
}

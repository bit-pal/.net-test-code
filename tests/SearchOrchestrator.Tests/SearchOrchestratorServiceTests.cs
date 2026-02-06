using Microsoft.Extensions.Logging;
using Moq;
using SearchOrchestrator.Core.Contracts;
using SearchOrchestrator.Core.Models;
using SearchOrchestrator.Core.Services;
using SearchOrchestrator.Infrastructure;
using Xunit;

namespace SearchOrchestrator.Tests;

public class SearchOrchestratorServiceTests
{
    private readonly IIndexingTaskStore _store;
    private readonly Mock<ISearchEngineClient> _searchEngineMock;
    private readonly Mock<ILogger<SearchOrchestratorService>> _loggerMock;
    private readonly ISearchOrchestratorService _orchestrator;

    public SearchOrchestratorServiceTests()
    {
        _store = new InMemoryIndexingTaskStore();
        _searchEngineMock = new Mock<ISearchEngineClient>();
        _loggerMock = new Mock<ILogger<SearchOrchestratorService>>();
        _orchestrator = new SearchOrchestratorService(_store, _searchEngineMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task StartIndexing_Rejects_EmptySourceIds()
    {
        var outcome = await _orchestrator.StartIndexingAsync(Array.Empty<string>()).ConfigureAwait(false);
        Assert.False(outcome.Accepted);
        Assert.NotNull(outcome.RejectionReason);
        Assert.Contains("empty", outcome.RejectionReason!, StringComparison.OrdinalIgnoreCase);
        _searchEngineMock.Verify(s => s.StartIndexingAsync(It.IsAny<IReadOnlyList<string>>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task StartIndexing_Rejects_UnknownSourceIds()
    {
        var outcome = await _orchestrator.StartIndexingAsync(new[] { "unknown-id" }).ConfigureAwait(false);
        Assert.False(outcome.Accepted);
        Assert.NotNull(outcome.RejectionReason);
        Assert.Contains("Unknown", outcome.RejectionReason!, StringComparison.OrdinalIgnoreCase);
        _searchEngineMock.Verify(s => s.StartIndexingAsync(It.IsAny<IReadOnlyList<string>>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task StartIndexing_Success_CreatesTask_And_CallsSearchEngine()
    {
        var sourceId = await _orchestrator.RegisterSourceAsync("external-1", "Source 1").ConfigureAwait(false);
        _searchEngineMock
            .Setup(s => s.StartIndexingAsync(It.IsAny<IReadOnlyList<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new IndexingSubmissionResult { Success = true, ExternalTaskId = "ext-123" });

        var outcome = await _orchestrator.StartIndexingAsync(new[] { sourceId }).ConfigureAwait(false);

        Assert.True(outcome.Accepted);
        Assert.False(outcome.IsExistingRunningTask);
        Assert.Equal(IndexingTaskStatus.Running, outcome.Task.Status);
        Assert.Equal("ext-123", outcome.Task.ExternalTaskId);
        _searchEngineMock.Verify(s => s.StartIndexingAsync(It.Is<IReadOnlyList<string>>(l => l.Count == 1 && l[0] == "external-1"), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task StartIndexing_Idempotent_ReturnsExistingRunningTask()
    {
        var sourceId = await _orchestrator.RegisterSourceAsync("external-1", null).ConfigureAwait(false);
        _searchEngineMock
            .Setup(s => s.StartIndexingAsync(It.IsAny<IReadOnlyList<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new IndexingSubmissionResult { Success = true, ExternalTaskId = "ext-1" });

        var first = await _orchestrator.StartIndexingAsync(new[] { sourceId }).ConfigureAwait(false);
        var second = await _orchestrator.StartIndexingAsync(new[] { sourceId }).ConfigureAwait(false);

        Assert.True(first.Accepted);
        Assert.True(second.Accepted);
        Assert.True(second.IsExistingRunningTask);
        Assert.Equal(first.Task.Id, second.Task.Id);
        _searchEngineMock.Verify(s => s.StartIndexingAsync(It.IsAny<IReadOnlyList<string>>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task StartIndexing_OnEngineTimeout_MarksTaskFailed()
    {
        var sourceId = await _orchestrator.RegisterSourceAsync("external-1", null).ConfigureAwait(false);
        _searchEngineMock
            .Setup(s => s.StartIndexingAsync(It.IsAny<IReadOnlyList<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new IndexingSubmissionResult { Success = false, IsTimeout = true });

        var outcome = await _orchestrator.StartIndexingAsync(new[] { sourceId }).ConfigureAwait(false);

        Assert.True(outcome.Accepted);
        Assert.Equal(IndexingTaskStatus.Failed, outcome.Task.Status);
        Assert.NotNull(outcome.Task.ErrorMessage);
        Assert.Contains("Timeout", outcome.Task.ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task StartIndexing_OnEngineFailure_MarksTaskFailed()
    {
        var sourceId = await _orchestrator.RegisterSourceAsync("external-1", null).ConfigureAwait(false);
        _searchEngineMock
            .Setup(s => s.StartIndexingAsync(It.IsAny<IReadOnlyList<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new IndexingSubmissionResult { Success = false, ErrorMessage = "Engine error" });

        var outcome = await _orchestrator.StartIndexingAsync(new[] { sourceId }).ConfigureAwait(false);

        Assert.True(outcome.Accepted);
        Assert.Equal(IndexingTaskStatus.Failed, outcome.Task.Status);
        Assert.Equal("Engine error", outcome.Task.ErrorMessage);
    }

    [Fact]
    public async Task StartIndexing_OnPartialSuccess_MarksPartiallyCompleted()
    {
        var sourceId = await _orchestrator.RegisterSourceAsync("external-1", null).ConfigureAwait(false);
        _searchEngineMock
            .Setup(s => s.StartIndexingAsync(It.IsAny<IReadOnlyList<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new IndexingSubmissionResult
            {
                Success = true,
                ExternalTaskId = "ext-p",
                IsPartialSuccess = true,
                PartialSuccessDetails = "Some sources failed."
            });

        var outcome = await _orchestrator.StartIndexingAsync(new[] { sourceId }).ConfigureAwait(false);

        Assert.True(outcome.Accepted);
        Assert.Equal(IndexingTaskStatus.PartiallyCompleted, outcome.Task.Status);
        Assert.Equal("Some sources failed.", outcome.Task.Details);
    }

    [Fact]
    public async Task GetTaskStatus_ReturnsTask_ById_And_ByCorrelationId()
    {
        var sourceId = await _orchestrator.RegisterSourceAsync("ext-1", null).ConfigureAwait(false);
        _searchEngineMock.Setup(s => s.StartIndexingAsync(It.IsAny<IReadOnlyList<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new IndexingSubmissionResult { Success = true, ExternalTaskId = "ext-1" });
        var outcome = await _orchestrator.StartIndexingAsync(new[] { sourceId }, "corr-123").ConfigureAwait(false);

        var byId = await _orchestrator.GetTaskStatusAsync(outcome.Task.Id).ConfigureAwait(false);
        var byCorr = await _orchestrator.GetTaskStatusAsync("corr-123").ConfigureAwait(false);

        Assert.NotNull(byId);
        Assert.NotNull(byCorr);
        Assert.Equal(byId.Id, byCorr!.Id);
        Assert.Equal("corr-123", byId.CorrelationId);
    }

    [Fact]
    public async Task CancelTask_UpdatesStatus_ToCancelled()
    {
        var sourceId = await _orchestrator.RegisterSourceAsync("ext-1", null).ConfigureAwait(false);
        _searchEngineMock.Setup(s => s.StartIndexingAsync(It.IsAny<IReadOnlyList<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new IndexingSubmissionResult { Success = true, ExternalTaskId = "ext-1" });
        _searchEngineMock.Setup(s => s.CancelIndexingAsync("ext-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CancelTaskResult { Success = true });
        var outcome = await _orchestrator.StartIndexingAsync(new[] { sourceId }).ConfigureAwait(false);

        var cancelResult = await _orchestrator.CancelTaskAsync(outcome.Task.Id).ConfigureAwait(false);

        Assert.True(cancelResult.Success);
        var task = await _orchestrator.GetTaskStatusAsync(outcome.Task.Id).ConfigureAwait(false);
        Assert.Equal(IndexingTaskStatus.Cancelled, task!.Status);
    }

    [Fact]
    public async Task Search_ReturnsResult_WithCorrelationId()
    {
        _searchEngineMock
            .Setup(s => s.SearchAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SearchResult
            {
                Query = "test",
                Items = new List<SearchResultItem> { new() { Id = "d1", Title = "Doc 1" } },
                TotalCount = 1
            });

        var result = await _orchestrator.SearchAsync("test", 10, "search-corr").ConfigureAwait(false);

        Assert.Equal("test", result.Query);
        Assert.Equal(1, result.TotalCount);
        Assert.Equal("search-corr", result.CorrelationId);
        Assert.Single(result.Items);
        Assert.Equal("d1", result.Items[0].Id);
    }
}

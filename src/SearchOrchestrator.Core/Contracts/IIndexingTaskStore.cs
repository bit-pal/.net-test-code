using SearchOrchestrator.Core.Models;

namespace SearchOrchestrator.Core.Contracts;

/// <summary>
/// Хранилище мета-информации о задачах и источниках индексации.
/// Реализация — in-memory или персистентное в Infrastructure.
/// </summary>
public interface IIndexingTaskStore
{
    Task<IndexingTask?> GetTaskByIdAsync(string taskId, CancellationToken cancellationToken = default);
    Task<IndexingTask?> GetTaskByCorrelationIdAsync(string correlationId, CancellationToken cancellationToken = default);
    Task<IndexingTask?> FindRunningTaskBySourceIdsAsync(IReadOnlyList<string> sourceIds, CancellationToken cancellationToken = default);
    Task<string> AddTaskAsync(IndexingTask task, CancellationToken cancellationToken = default);
    Task UpdateTaskAsync(IndexingTask task, CancellationToken cancellationToken = default);

    Task<IndexingSource?> GetSourceByIdAsync(string sourceId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<IndexingSource>> GetSourcesByIdsAsync(IReadOnlyList<string> sourceIds, CancellationToken cancellationToken = default);
    Task<string> AddOrUpdateSourceAsync(IndexingSource source, CancellationToken cancellationToken = default);
}

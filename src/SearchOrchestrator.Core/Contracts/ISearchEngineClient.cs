using SearchOrchestrator.Core.Models;

namespace SearchOrchestrator.Core.Contracts;

/// <summary>
/// Контракт клиента внешнего сервиса поиска.
/// Реализация (реальная или mock) — в Infrastructure.
/// </summary>
public interface ISearchEngineClient
{
    /// <summary>
    /// Запускает индексацию указанных источников.
    /// Возвращает идентификатор задачи во внешнем сервисе или null при немедленной ошибке.
    /// </summary>
    /// <param name="sourceExternalIds">Идентификаторы источников для индексации.</param>
    /// <param name="cancellationToken">Отмена операции.</param>
    /// <returns>Результат постановки задачи (успех, таймаут, ошибка, частичный успех).</returns>
    Task<IndexingSubmissionResult> StartIndexingAsync(
        IReadOnlyList<string> sourceExternalIds,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Получает статус задачи индексации во внешнем сервисе.
    /// </summary>
    Task<ExternalTaskStatusResult> GetIndexingStatusAsync(
        string externalTaskId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Отменяет задачу индексации во внешнем сервисе (если поддерживается).
    /// </summary>
    Task<CancelTaskResult> CancelIndexingAsync(
        string externalTaskId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Выполняет поиск по строке запроса.
    /// </summary>
    Task<SearchResult> SearchAsync(
        string query,
        int maxResults = 100,
        CancellationToken cancellationToken = default);
}

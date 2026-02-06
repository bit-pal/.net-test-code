using SearchOrchestrator.Core.Models;

namespace SearchOrchestrator.Core.Services;

/// <summary>
/// Сервис оркестрации: постановка задач индексации, получение статуса, поиск.
/// Граница ответственности: идемпотентность, обработка ошибок внешнего сервиса, мета-данные.
/// </summary>
public interface ISearchOrchestratorService
{
    /// <summary>
    /// Запускает индексацию источников. Идемпотентность: при повторном запросе с теми же sourceIds
    /// и уже выполняющейся задачей возвращается существующая задача.
    /// </summary>
    /// <param name="sourceIds">Идентификаторы источников (должны существовать в store).</param>
    /// <param name="correlationId">Корреляционный идентификатор запроса (для логов и идемпотентности по желанию).</param>
    /// <param name="cancellationToken">Отмена.</param>
    Task<StartIndexingOutcome> StartIndexingAsync(
        IReadOnlyList<string> sourceIds,
        string? correlationId = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Возвращает статус задачи по идентификатору задачи или по correlationId.
    /// </summary>
    Task<IndexingTask?> GetTaskStatusAsync(string taskIdOrCorrelationId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Отменяет задачу (в нашем store и по возможности во внешнем сервисе).
    /// </summary>
    Task<CancelTaskResult> CancelTaskAsync(string taskId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Поиск по строке через внешний сервис. Ошибки/таймауты пробрасываются с обогащением.
    /// </summary>
    Task<SearchResult> SearchAsync(
        string query,
        int maxResults = 100,
        string? correlationId = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Регистрирует или обновляет источник. Возвращает Id источника.
    /// </summary>
    Task<string> RegisterSourceAsync(string externalId, string? displayName, CancellationToken cancellationToken = default);
}

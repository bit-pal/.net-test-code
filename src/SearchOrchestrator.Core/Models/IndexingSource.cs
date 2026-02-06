namespace SearchOrchestrator.Core.Models;

/// <summary>
/// Источник для индексации (путь, URI и т.п. — описание для внешнего сервиса).
/// Реальное чтение файлов — зона ответственности внешнего сервиса.
/// </summary>
public class IndexingSource
{
    public required string Id { get; init; }
    /// <summary>Идентификатор для внешнего сервиса (путь, URI, ключ).</summary>
    public required string ExternalId { get; init; }
    public string? DisplayName { get; set; }
    public DateTime? LastIndexedAtUtc { get; set; }
    public string? LastTaskId { get; set; }
    public DateTime CreatedAtUtc { get; init; }
}

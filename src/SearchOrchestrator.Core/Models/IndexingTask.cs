namespace SearchOrchestrator.Core.Models;

/// <summary>
/// Задача на индексацию одного или нескольких источников.
/// Минимальная мета-информация для оркестратора.
/// </summary>
public class IndexingTask
{
    public required string Id { get; init; }
    public required string CorrelationId { get; init; }
    public required IReadOnlyList<string> SourceIds { get; init; }
    public IndexingTaskStatus Status { get; set; }
    public DateTime CreatedAtUtc { get; init; }
    public DateTime? StartedAtUtc { get; set; }
    public DateTime? CompletedAtUtc { get; set; }
    public string? ErrorMessage { get; set; }
    /// <summary>Детали при частичном успехе (например, какие источники не удались).</summary>
    public string? Details { get; set; }
    /// <summary>Внешний идентификатор задачи в Search Engine (если есть).</summary>
    public string? ExternalTaskId { get; set; }
}

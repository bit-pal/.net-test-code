namespace SearchOrchestrator.Core.Models;

/// <summary>
/// Результат запроса статуса задачи во внешнем сервисе.
/// </summary>
public class ExternalTaskStatusResult
{
    public bool Success { get; init; }
    public IndexingTaskStatus? Status { get; init; }
    public string? ErrorMessage { get; init; }
    public bool IsTimeout { get; init; }
    public string? Details { get; init; }
}

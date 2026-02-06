namespace SearchOrchestrator.Core.Models;

/// <summary>
/// Результат отмены задачи во внешнем сервисе.
/// </summary>
public class CancelTaskResult
{
    public bool Success { get; init; }
    public string? ErrorMessage { get; init; }
    public bool IsTimeout { get; init; }
}

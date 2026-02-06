using SearchOrchestrator.Core.Models;

namespace SearchOrchestrator.Core.Models;

/// <summary>
/// Результат запуска индексации (идемпотентный ответ).
/// </summary>
public class StartIndexingOutcome
{
    public bool Accepted { get; init; }
    public required IndexingTask Task { get; init; }
    /// <summary>True, если возвращена уже существующая выполняющаяся задача (идемпотентность).</summary>
    public bool IsExistingRunningTask { get; init; }
    public string? RejectionReason { get; init; }
}

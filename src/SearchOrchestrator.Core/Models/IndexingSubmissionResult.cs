namespace SearchOrchestrator.Core.Models;

/// <summary>
/// Результат постановки задачи индексации во внешний сервис.
/// </summary>
public class IndexingSubmissionResult
{
    public bool Success { get; init; }
    public string? ExternalTaskId { get; init; }
    public string? ErrorMessage { get; init; }
    public bool IsTimeout { get; init; }
    /// <summary>Частичный успех: задача принята, но с предупреждениями по части источников.</summary>
    public bool IsPartialSuccess { get; init; }
    public string? PartialSuccessDetails { get; init; }
}

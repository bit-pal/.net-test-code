namespace SearchOrchestrator.Core.Models;

/// <summary>
/// Статус задачи индексации.
/// </summary>
public enum IndexingTaskStatus
{
    /// <summary>Задача создана, ожидает постановки во внешний сервис.</summary>
    Pending = 0,

    /// <summary>Задача передана во внешний сервис, выполняется.</summary>
    Running = 1,

    /// <summary>Индексация успешно завершена.</summary>
    Completed = 2,

    /// <summary>Задача отменена (пользователем или по таймауту).</summary>
    Cancelled = 3,

    /// <summary>Завершена с ошибкой.</summary>
    Failed = 4,

    /// <summary>Частичный успех (часть источников проиндексирована, часть — с ошибками).</summary>
    PartiallyCompleted = 5
}

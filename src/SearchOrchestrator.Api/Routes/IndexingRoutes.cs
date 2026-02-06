using Microsoft.AspNetCore.Mvc;
using SearchOrchestrator.Core.Models;
using SearchOrchestrator.Core.Services;

namespace SearchOrchestrator.Api.Routes;

/// <summary>
/// Контракты API: запуск индексации, статус задачи, отмена.
/// </summary>
public static class IndexingRoutes
{
    public static void Map(WebApplication app)
    {
        var group = app.MapGroup("/api/index").WithTags("Indexing");

        group.MapPost("/start", StartIndexing)
            .WithName("StartIndexing")
            .Produces<StartIndexingResponse>(StatusCodes.Status200OK)
            .Produces<StartIndexingResponse>(StatusCodes.Status400BadRequest);

        group.MapGet("/status/{id}", GetStatus)
            .WithName("GetIndexingStatus")
            .Produces<IndexingTaskStatusResponse>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound);

        group.MapPost("/tasks/{id}/cancel", CancelTask)
            .WithName("CancelTask")
            .Produces<CancelTaskResponse>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound);
    }

    private static async Task<IResult> StartIndexing(
        [FromBody] StartIndexingRequest request,
        [FromServices] ISearchOrchestratorService orchestrator,
        HttpContext httpContext)
    {
        var correlationId = (string?)httpContext.Items["CorrelationId"] ?? httpContext.TraceIdentifier;
        var outcome = await orchestrator.StartIndexingAsync(
            request.SourceIds,
            request.CorrelationId ?? correlationId,
            httpContext.RequestAborted).ConfigureAwait(false);

        var response = new StartIndexingResponse
        {
            Accepted = outcome.Accepted,
            TaskId = outcome.Task.Id,
            CorrelationId = outcome.Task.CorrelationId,
            Status = outcome.Task.Status.ToString(),
            IsExistingRunningTask = outcome.IsExistingRunningTask,
            RejectionReason = outcome.RejectionReason,
            ErrorMessage = outcome.Task.ErrorMessage
        };
        return Results.Ok(response);
    }

    private static async Task<IResult> GetStatus(
        string id,
        [FromServices] ISearchOrchestratorService orchestrator,
        CancellationToken cancellationToken)
    {
        var task = await orchestrator.GetTaskStatusAsync(id, cancellationToken).ConfigureAwait(false);
        if (task == null)
            return Results.NotFound();
        return Results.Ok(new IndexingTaskStatusResponse
        {
            TaskId = task.Id,
            CorrelationId = task.CorrelationId,
            Status = task.Status.ToString(),
            SourceIds = task.SourceIds,
            CreatedAtUtc = task.CreatedAtUtc,
            StartedAtUtc = task.StartedAtUtc,
            CompletedAtUtc = task.CompletedAtUtc,
            ErrorMessage = task.ErrorMessage,
            Details = task.Details,
            ExternalTaskId = task.ExternalTaskId
        });
    }

    private static async Task<IResult> CancelTask(
        string id,
        [FromServices] ISearchOrchestratorService orchestrator,
        CancellationToken cancellationToken)
    {
        var result = await orchestrator.CancelTaskAsync(id, cancellationToken).ConfigureAwait(false);
        if (!result.Success && result.ErrorMessage?.Contains("not found") == true)
            return Results.NotFound();
        return Results.Ok(new CancelTaskResponse
        {
            Success = result.Success,
            ErrorMessage = result.ErrorMessage
        });
    }
}

public record StartIndexingRequest
{
    public required IReadOnlyList<string> SourceIds { get; init; }
    public string? CorrelationId { get; init; }
}

public record StartIndexingResponse
{
    public bool Accepted { get; init; }
    public string TaskId { get; init; } = "";
    public string CorrelationId { get; init; } = "";
    public string Status { get; init; } = "";
    public bool IsExistingRunningTask { get; init; }
    public string? RejectionReason { get; init; }
    public string? ErrorMessage { get; init; }
}

public record IndexingTaskStatusResponse
{
    public string TaskId { get; init; } = "";
    public string CorrelationId { get; init; } = "";
    public string Status { get; init; } = "";
    public IReadOnlyList<string> SourceIds { get; init; } = Array.Empty<string>();
    public DateTime CreatedAtUtc { get; init; }
    public DateTime? StartedAtUtc { get; init; }
    public DateTime? CompletedAtUtc { get; init; }
    public string? ErrorMessage { get; init; }
    public string? Details { get; init; }
    public string? ExternalTaskId { get; init; }
}

public record CancelTaskResponse
{
    public bool Success { get; init; }
    public string? ErrorMessage { get; init; }
}

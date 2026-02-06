using SearchOrchestrator.Core.Services;

namespace SearchOrchestrator.Api.Routes;

public static class SourceRoutes
{
    public static void Map(WebApplication app)
    {
        var group = app.MapGroup("/api/sources").WithTags("Sources");

        group.MapPost("", RegisterSource)
            .WithName("RegisterSource")
            .Produces<RegisterSourceResponse>(StatusCodes.Status200OK);
    }

    private static async Task<IResult> RegisterSource(
        [FromBody] RegisterSourceRequest request,
        [FromServices] ISearchOrchestratorService orchestrator,
        CancellationToken cancellationToken)
    {
        var id = await orchestrator.RegisterSourceAsync(
            request.ExternalId,
            request.DisplayName,
            cancellationToken).ConfigureAwait(false);
        return Results.Ok(new RegisterSourceResponse { SourceId = id });
    }
}

public record RegisterSourceRequest
{
    public required string ExternalId { get; init; }
    public string? DisplayName { get; init; }
}

public record RegisterSourceResponse
{
    public string SourceId { get; init; } = "";
}

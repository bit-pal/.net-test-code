using Microsoft.AspNetCore.Mvc;
using SearchOrchestrator.Core.Models;
using SearchOrchestrator.Core.Services;

namespace SearchOrchestrator.Api.Routes;

public static class SearchRoutes
{
    public static void Map(WebApplication app)
    {
        var group = app.MapGroup("/api/search").WithTags("Search");

        group.MapGet("", Search)
            .WithName("Search")
            .Produces<SearchResponse>(StatusCodes.Status200OK);
    }

    private static async Task<IResult> Search(
        [FromQuery] string q,
        [FromQuery] int max = 100,
        [FromServices] ISearchOrchestratorService orchestrator,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(q))
            return Results.BadRequest(new { error = "Query 'q' is required." });

        var correlationId = (string?)httpContext.Items["CorrelationId"] ?? httpContext.TraceIdentifier;
        var result = await orchestrator.SearchAsync(q, max, correlationId, cancellationToken).ConfigureAwait(false);
        return Results.Ok(new SearchResponse
        {
            Query = result.Query,
            TotalCount = result.TotalCount,
            CorrelationId = result.CorrelationId,
            Items = result.Items.Select(x => new SearchItemDto
            {
                Id = x.Id,
                Title = x.Title,
                Snippet = x.Snippet,
                SourceId = x.SourceId,
                Metadata = x.Metadata
            }).ToList()
        });
    }
}

public record SearchResponse
{
    public string Query { get; init; } = "";
    public int TotalCount { get; init; }
    public string? CorrelationId { get; init; }
    public IReadOnlyList<SearchItemDto> Items { get; init; } = Array.Empty<SearchItemDto>();
}

public record SearchItemDto
{
    public string Id { get; init; } = "";
    public string? Title { get; init; }
    public string? Snippet { get; init; }
    public string? SourceId { get; init; }
    public Dictionary<string, object?>? Metadata { get; init; }
}

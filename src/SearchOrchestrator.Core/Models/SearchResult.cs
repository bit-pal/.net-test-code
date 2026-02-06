namespace SearchOrchestrator.Core.Models;

/// <summary>
/// Один результат поиска (документ/файл из индекса).
/// </summary>
public class SearchResultItem
{
    public required string Id { get; init; }
    public string? Title { get; set; }
    public string? Snippet { get; set; }
    public string? SourceId { get; set; }
    public Dictionary<string, object?>? Metadata { get; set; }
}

/// <summary>
/// Ответ поиска от оркестратора.
/// </summary>
public class SearchResult
{
    public required string Query { get; init; }
    public required IReadOnlyList<SearchResultItem> Items { get; init; }
    public int TotalCount { get; init; }
    public string? CorrelationId { get; init; }
}

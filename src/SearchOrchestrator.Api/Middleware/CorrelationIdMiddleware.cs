namespace SearchOrchestrator.Api.Middleware;

/// <summary>
/// Устанавливает или прокидывает корреляционный идентификатор запроса для наблюдаемости.
/// </summary>
public class CorrelationIdMiddleware
{
    public const string HeaderName = "X-Correlation-ID";
    private readonly RequestDelegate _next;

    public CorrelationIdMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = context.Request.Headers[HeaderName].FirstOrDefault()
            ?? context.TraceIdentifier
            ?? Guid.NewGuid().ToString("N");
        context.TraceIdentifier = correlationId;
        context.Response.Headers[HeaderName] = correlationId;
        context.Items["CorrelationId"] = correlationId;
        await _next(context).ConfigureAwait(false);
    }
}

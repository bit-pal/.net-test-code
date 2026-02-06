using Microsoft.AspNetCore.Http;
using SearchOrchestrator.Api.Middleware;
using SearchOrchestrator.Api.Routes;
using SearchOrchestrator.Core.Extensions;
using SearchOrchestrator.Infrastructure.Extensions;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((ctx, cfg) =>
{
    cfg.ReadFrom.Configuration(ctx.Configuration)
        .Enrich.FromLogContext()
        .Enrich.WithProperty("Application", "SearchOrchestrator.Api")
        .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {CorrelationId} {Message:lj}{NewLine}{Exception}");
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "Search Orchestrator API", Version = "v1" });
});

builder.Services.AddSearchOrchestratorCore();
builder.Services.AddInfrastructure(builder.Configuration);

var app = builder.Build();

app.UseMiddleware<CorrelationIdMiddleware>();
app.UseSerilogRequestLogging(options =>
{
    options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
    {
        diagnosticContext.Set("CorrelationId", httpContext.TraceIdentifier);
    };
});

app.UseSwagger();
app.UseSwaggerUI();

IndexingRoutes.Map(app);
SearchRoutes.Map(app);
SourceRoutes.Map(app);

app.Run();

using Microsoft.Extensions.DependencyInjection;
using SearchOrchestrator.Core.Services;

namespace SearchOrchestrator.Core.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddSearchOrchestratorCore(this IServiceCollection services)
    {
        services.AddScoped<ISearchOrchestratorService, SearchOrchestratorService>();
        return services;
    }
}

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SearchOrchestrator.Core.Contracts;
using SearchOrchestrator.Infrastructure;

namespace SearchOrchestrator.Infrastructure.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton<IIndexingTaskStore, InMemoryIndexingTaskStore>();
        services.Configure<MockSearchEngineOptions>(configuration.GetSection(MockSearchEngineOptions.SectionName));
        services.AddSingleton<ISearchEngineClient, MockSearchEngineClient>();
        return services;
    }
}

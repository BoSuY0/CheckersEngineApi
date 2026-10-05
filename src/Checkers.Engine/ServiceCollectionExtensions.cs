using Checkers.Application;
using Checkers.Application.Ports;
using Checkers.Engine.Connections;
using Checkers.Engine.Workers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Checkers.Engine;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Binds the "Engine" section and registers the KingsRow worker pool as the application's
    /// <see cref="IEngineWorkerPool"/> and as a hosted service that starts and warms up the workers.
    /// </summary>
    public static IServiceCollection AddKingsRowEngine(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddValidatedOptions<EngineOptions>(configuration, EngineOptions.SectionName);

        services.AddSingleton<IHostConnectionFactory, ProcessHostConnectionFactory>();
        services.AddSingleton<KingsRowWorkerPool>();
        services.AddSingleton<IEngineWorkerPool>(provider => provider.GetRequiredService<KingsRowWorkerPool>());
        services.AddHostedService(provider => provider.GetRequiredService<KingsRowWorkerPool>());
        return services;
    }
}

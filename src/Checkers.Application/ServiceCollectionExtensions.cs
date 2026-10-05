using Checkers.Application.Caching;
using Checkers.Application.Health;
using Checkers.Application.Moves;
using Checkers.Application.Strength;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Checkers.Application;

public static class ServiceCollectionExtensions
{
    /// <summary>Registers the use cases and binds the "Limits" and "Cache" sections.</summary>
    public static IServiceCollection AddCheckersApplication(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddValidatedOptions<LimitsOptions>(configuration, LimitsOptions.SectionName);
        services.AddValidatedOptions<CacheOptions>(configuration, CacheOptions.SectionName);
        services.TryAddSingleton(TimeProvider.System);
        services.AddSingleton<StrengthPolicy>();
        services.AddSingleton<SuggestMoveService>();
        services.AddSingleton<ValidateMoveService>();
        services.AddSingleton<HealthService>();
        return services;
    }

    /// <summary>Binds <typeparamref name="TOptions"/> to a configuration section and validates its data annotations at startup.</summary>
    public static void AddValidatedOptions<TOptions>(
        this IServiceCollection services,
        IConfiguration configuration,
        string sectionName)
        where TOptions : class =>
        services.AddOptions<TOptions>()
            .Bind(configuration.GetSection(sectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();
}

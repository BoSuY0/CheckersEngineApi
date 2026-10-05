using Checkers.Application.Ports;
using Checkers.Engine;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Checkers.Api.Tests;

/// <summary>
/// Hosts the API with <see cref="Pool"/> in place of the KingsRow worker processes, and records its log in
/// <see cref="Log"/>.
/// </summary>
internal sealed class CheckersApiFactory : WebApplicationFactory<Program>
{
    public FakeEngineWorkerPool Pool { get; } = new();

    public CapturingLoggerProvider Log { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder) =>
        builder.ConfigureTestServices(services =>
        {
            // Only the engine's hosted service goes: the web host itself also runs as a hosted service.
            foreach (var engineService in services.Where(IsEngineHostedService).ToList())
            {
                services.Remove(engineService);
            }

            services.RemoveAll<IEngineWorkerPool>();
            services.AddSingleton<IEngineWorkerPool>(Pool);
            services.AddSingleton<ILoggerProvider>(Log);
        });

    private static bool IsEngineHostedService(ServiceDescriptor descriptor) =>
        descriptor.ServiceType == typeof(IHostedService)
        && (descriptor.ImplementationType ?? descriptor.ImplementationFactory?.Method.DeclaringType)?.Assembly
        == typeof(EngineOptions).Assembly;
}

using Checkers.Application.Ports;
using Checkers.Engine.Connections;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Checkers.Engine.Workers;

/// <summary>The configured number of KingsRow workers, started and warmed up with the application.</summary>
internal sealed class KingsRowWorkerPool : IEngineWorkerPool, IHostedService, IAsyncDisposable
{
    private readonly EngineWorker[] _workers;
    private int _next = -1;

    public KingsRowWorkerPool(
        IHostConnectionFactory connectionFactory,
        IOptions<EngineOptions> options,
        ILoggerFactory loggerFactory)
    {
        EngineName = options.Value.Type;
        var logger = loggerFactory.CreateLogger<EngineWorker>();
        _workers =
        [
            .. Enumerable.Range(1, options.Value.Workers)
                .Select(id => new EngineWorker(id, connectionFactory, logger)),
        ];
    }

    public string EngineName { get; }

    public int WorkerCount => _workers.Length;

    public int ReadyWorkerCount => _workers.Count(worker => worker.IsReady);

    public async Task<IEngineSession> AcquireAsync(CancellationToken cancellationToken)
    {
        var turn = (uint)Interlocked.Increment(ref _next);
        return await _workers[turn % (uint)_workers.Length].AcquireAsync(cancellationToken);
    }

    /// <summary>Starts and warms up every worker; a worker that fails to start fails the application start.</summary>
    public Task StartAsync(CancellationToken cancellationToken) =>
        Task.WhenAll(_workers.Select(worker => worker.StartAsync(cancellationToken)));

    // The hosts are terminated on disposal, which also covers a pool whose start failed part-way.
    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    public async ValueTask DisposeAsync()
    {
        foreach (var worker in _workers)
        {
            await worker.DisposeAsync();
        }
    }
}

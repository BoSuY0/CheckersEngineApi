using Checkers.Application.Ports;
using Checkers.Domain;

namespace Checkers.Api.Tests;

/// <summary>A pool of two workers whose engine answers with <see cref="Result"/> after <see cref="SearchDelay"/>.</summary>
internal sealed class FakeEngineWorkerPool : IEngineWorkerPool
{
    public string EngineName => "chinook";

    public int WorkerCount => 2;

    public int ReadyWorkerCount { get; set; } = 2;

    public SearchResult Result { get; set; } = new("16x23", ["16x23"], 35, 4096, 12, false);

    public TimeSpan SearchDelay { get; set; } = TimeSpan.Zero;

    public Task<IEngineSession> AcquireAsync(CancellationToken cancellationToken) =>
        Task.FromResult<IEngineSession>(new Session(this));

    private sealed class Session(FakeEngineWorkerPool pool) : IEngineSession
    {
        public Task SetPositionAsync(Position position, CancellationToken cancellationToken) => Task.CompletedTask;

        public async Task<SearchResult> SearchAsync(SearchLimits limits, CancellationToken cancellationToken)
        {
            await Task.Delay(pool.SearchDelay, cancellationToken);
            return pool.Result;
        }

        public Task<IReadOnlyList<Wdl>> ProbeAsync(IReadOnlyList<Position> positions, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<Wdl>>([.. positions.Select(_ => Wdl.Unknown)]);

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }
}

using Checkers.Application.Ports;
using Checkers.Domain;

namespace Checkers.Application.Tests.Fakes;

/// <summary>
/// A pool whose sessions answer every search with <see cref="Result"/>, or fail it when <see cref="FailsSearches"/>,
/// and probe <see cref="Tablebase"/>.
/// </summary>
internal sealed class FakeEnginePool : IEngineWorkerPool
{
    public SearchResult? Result { get; init; }

    public bool FailsSearches { get; init; }

    public ITablebase Tablebase { get; init; } = new FakeTablebase();

    public string EngineName => "test-engine";

    public int WorkerCount { get; init; } = 2;

    public int ReadyWorkerCount { get; init; } = 2;

    public int Acquisitions { get; private set; }

    public int Releases { get; private set; }

    /// <summary>Each search with the position that was set before it.</summary>
    public List<(Position Position, SearchLimits Limits)> Searches { get; } = [];

    public Task<IEngineSession> AcquireAsync(CancellationToken cancellationToken)
    {
        Acquisitions++;
        return Task.FromResult<IEngineSession>(new Session(this));
    }

    private sealed class Session(FakeEnginePool pool) : IEngineSession
    {
        private Position? _position;

        public Task SetPositionAsync(Position position, CancellationToken cancellationToken)
        {
            _position = position;
            return Task.CompletedTask;
        }

        public Task<SearchResult> SearchAsync(SearchLimits limits, CancellationToken cancellationToken)
        {
            Assert.NotNull(_position);
            if (pool.FailsSearches)
            {
                throw new EngineFailureException("The engine reported an error.");
            }

            Assert.NotNull(pool.Result);
            pool.Searches.Add((_position, limits));
            return Task.FromResult(pool.Result);
        }

        public Task<IReadOnlyList<Wdl>> ProbeAsync(IReadOnlyList<Position> positions, CancellationToken cancellationToken) =>
            pool.Tablebase.ProbeAsync(positions, cancellationToken);

        public ValueTask DisposeAsync()
        {
            pool.Releases++;
            return ValueTask.CompletedTask;
        }
    }
}

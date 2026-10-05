using Checkers.Application.Ports;
using Checkers.Domain;
using Checkers.Engine.Protocol;

namespace Checkers.Engine.Workers;

/// <summary>The lease of one worker; disposing it returns the worker to the pool.</summary>
internal sealed class EngineSession(EngineWorker worker) : IEngineSession
{
    private int _released;

    private EngineWorker Worker
    {
        get
        {
            ObjectDisposedException.ThrowIf(_released != 0, this);
            return worker;
        }
    }

    public async Task SetPositionAsync(Position position, CancellationToken cancellationToken) =>
        await Worker.ExchangeAsync<PositionSetResponse>(new SetPositionRequest(position.ToBoard()), cancellationToken);

    public async Task<SearchResult> SearchAsync(SearchLimits limits, CancellationToken cancellationToken)
    {
        var response = await Worker.ExchangeAsync<SearchResponse>(limits.ToSearchRequest(), cancellationToken);
        return response.ToSearchResult();
    }

    public async Task<IReadOnlyList<Wdl>> ProbeAsync(
        IReadOnlyList<Position> positions,
        CancellationToken cancellationToken)
    {
        var request = new ProbeRequest([.. positions.Select(position => position.ToBoard())]);
        var response = await Worker.ExchangeAsync<ProbeResponse>(request, cancellationToken);
        return [.. response.Values.Select(value => value.ToWdl())];
    }

    public ValueTask DisposeAsync()
    {
        if (Interlocked.Exchange(ref _released, 1) == 0)
        {
            worker.Release();
        }

        return ValueTask.CompletedTask;
    }
}

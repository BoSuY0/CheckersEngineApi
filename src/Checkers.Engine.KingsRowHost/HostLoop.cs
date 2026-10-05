using System.Diagnostics;
using System.Threading.Channels;
using Checkers.Engine.Protocol;

namespace Checkers.Engine.KingsRowHost;

/// <summary>Answers requests one at a time until the input ends. While a search runs, only stop requests may arrive.</summary>
internal sealed class HostLoop(
    JsonLinesReader<HostRequest> requests,
    JsonLinesWriter<HostResponse> responses,
    KingsRowEngine engine,
    EndgameDatabase database)
{
    // Requests are read ahead into this inbox so that a stop request is seen while a search is running.
    private readonly Channel<HostRequest> _inbox = Channel.CreateUnbounded<HostRequest>();

    /// <exception cref="ProtocolException">A malformed line, or a request other than stop during a search.</exception>
    public async Task RunAsync()
    {
        var receiving = Task.Run(ReceiveAsync);
        await foreach (var request in _inbox.Reader.ReadAllAsync())
        {
            // A stop request while idle has nothing to stop.
            if (request is not StopRequest)
            {
                await responses.WriteAsync(await AnswerAsync(request), CancellationToken.None);
            }
        }

        await receiving;
    }

    private async Task ReceiveAsync()
    {
        try
        {
            while (await requests.ReadAsync(CancellationToken.None) is { } request)
            {
                _inbox.Writer.TryWrite(request);
            }

            _inbox.Writer.TryComplete();
        }
        catch (Exception exception)
        {
            _inbox.Writer.TryComplete(exception);
        }
    }

    private async Task<HostResponse> AnswerAsync(HostRequest request)
    {
        try
        {
            return request switch
            {
                SetPositionRequest setPosition => SetPosition(setPosition.Board),
                SearchRequest search => await SearchAsync(search),
                ProbeRequest probe => new ProbeResponse([.. probe.Boards.Select(database.Probe)]),
                _ => throw new UnreachableException($"Unexpected request {request}."),
            };
        }
        catch (HostRequestException exception)
        {
            return new ErrorResponse(exception.Message);
        }
    }

    private PositionSetResponse SetPosition(Board board)
    {
        engine.SetPosition(board);
        return new PositionSetResponse();
    }

    private async Task<SearchResponse> SearchAsync(SearchRequest request)
    {
        using var stop = new CancellationTokenSource();
        var search = engine.SearchAsync(request, stop.Token);
        var arrival = _inbox.Reader.WaitToReadAsync().AsTask();
        while (await Task.WhenAny(search, arrival) == arrival)
        {
            if (!await arrival)
            {
                // The input ended, so nobody waits for the result.
                await stop.CancelAsync();
                break;
            }

            _inbox.Reader.TryRead(out var message);
            if (message is not StopRequest)
            {
                throw new ProtocolException($"Only stop is accepted during a search, received {message}.");
            }

            await stop.CancelAsync();
            arrival = _inbox.Reader.WaitToReadAsync().AsTask();
        }

        return await search;
    }
}

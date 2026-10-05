using Checkers.Application.Ports;
using Checkers.Domain;
using Checkers.Engine.Protocol;
using Checkers.Engine.Tests.Fakes;
using Checkers.Engine.Workers;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Checkers.Engine.Tests.Workers;

public sealed class KingsRowWorkerPoolTests
{
    private static readonly SearchLimits Limits = new(TimeSpan.FromMilliseconds(100), null);

    private static CancellationToken Token => TestContext.Current.CancellationToken;

    [Fact]
    public async Task StartAsync_WarmsUpEveryWorker()
    {
        var host = new ScriptedHost();
        await using var pool = await StartPoolAsync(host, workers: 2);

        Assert.Equal(2, pool.ReadyWorkerCount);
        Assert.Equal(2, host.Connections.Count);
        Assert.All(host.Connections, connection => Assert.Collection(
            connection.Requests,
            request => Assert.Equivalent(Position.Initial.ToBoard(), Assert.IsType<SetPositionRequest>(request).Board),
            request => Assert.IsType<SearchRequest>(request)));
    }

    [Fact]
    public async Task AcquireAsync_RoutesSessionsRoundRobin()
    {
        var host = new ScriptedHost();
        await using var pool = await StartPoolAsync(host, workers: 2);

        foreach (var square in new[] { 1, 2, 3, 4 })
        {
            await using var session = await pool.AcquireAsync(Token);
            await session.SetPositionAsync(Position.Parse($"B:W32:B{square}"), Token);
        }

        var squaresPerHost = host.Connections
            .Select(connection => connection.Requests
                .OfType<SetPositionRequest>()
                .Skip(1)
                .Select(request => request.Board.BlackMen.Single())
                .ToArray())
            .OrderBy(squares => squares[0]);
        Assert.Equal([[1, 3], [2, 4]], squaresPerHost);
    }

    [Fact]
    public async Task AcquireAsync_WaitsUntilTheWorkerIsReleased()
    {
        await using var pool = await StartPoolAsync(new ScriptedHost(), workers: 1);
        var first = await pool.AcquireAsync(Token);

        var second = pool.AcquireAsync(Token);

        Assert.False(second.IsCompleted);
        await first.DisposeAsync();
        await (await second).DisposeAsync();
    }

    [Fact]
    public async Task AcquireAsync_StopsWaitingWhenCancelled()
    {
        await using var pool = await StartPoolAsync(new ScriptedHost(), workers: 1);
        var first = await pool.AcquireAsync(Token);
        using var deadline = new CancellationTokenSource();

        var waiting = pool.AcquireAsync(deadline.Token);
        await deadline.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => waiting);
        await first.DisposeAsync();
        await (await pool.AcquireAsync(Token)).DisposeAsync();
    }

    [Fact]
    public async Task DisposeAsync_ReleasesTheWorkerOnlyOnce()
    {
        await using var pool = await StartPoolAsync(new ScriptedHost(), workers: 1);
        var released = await pool.AcquireAsync(Token);
        await released.DisposeAsync();
        await released.DisposeAsync();

        var current = await pool.AcquireAsync(Token);
        var next = pool.AcquireAsync(Token);

        Assert.False(next.IsCompleted);
        await current.DisposeAsync();
        await (await next).DisposeAsync();
    }

    [Fact]
    public async Task ProbeAsync_ReturnsOneValuePerPositionInOrder()
    {
        var host = new ScriptedHost
        {
            OnRequest = (connection, request) =>
            {
                if (request is ProbeRequest)
                {
                    connection.Reply(new ProbeResponse([TablebaseValue.Loss, TablebaseValue.Draw]));
                }
                else
                {
                    ScriptedHost.AnswerNormally(connection, request);
                }
            },
        };
        await using var pool = await StartPoolAsync(host, workers: 1);
        await using var session = await pool.AcquireAsync(Token);
        Position[] positions = [Position.Parse("W:W32:B1"), Position.Parse("B:WK10:BK20")];

        var values = await session.ProbeAsync(positions, Token);

        Assert.Equal([Wdl.Loss, Wdl.Draw], values);
        var probe = Assert.IsType<ProbeRequest>(host.Connections.Single().Requests[^1]);
        Assert.Equivalent(positions.Select(position => position.ToBoard()), probe.Boards, strict: true);
    }

    [Fact]
    public async Task CancelledSearch_IsStoppedAndTheNextSessionWaitsForTheHostToAnswer()
    {
        var host = new ScriptedHost();
        await using var pool = await StartPoolAsync(host, workers: 1);
        var stopReceived = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        host.OnRequest = (connection, request) =>
        {
            if (request is StopRequest)
            {
                stopReceived.TrySetResult();
            }
            else if (request is not SearchRequest)
            {
                ScriptedHost.AnswerNormally(connection, request);
            }
        };
        var session = await pool.AcquireAsync(Token);
        using var deadline = new CancellationTokenSource();

        var search = session.SearchAsync(Limits, deadline.Token);
        await deadline.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => search);
        await stopReceived.Task.WaitAsync(Token);
        await session.DisposeAsync();
        var next = pool.AcquireAsync(Token);
        Assert.False(next.IsCompleted);

        host.Connections.Single().Reply(ScriptedHost.SearchAnswer);

        await (await next).DisposeAsync();
        Assert.Single(host.Connections);
        Assert.Equal(1, pool.ReadyWorkerCount);
    }

    [Fact]
    public async Task CancelledSearch_RestartsTheHostWhenItDoesNotStop()
    {
        var host = new ScriptedHost();
        await using var pool = await StartPoolAsync(host, workers: 1);
        host.OnRequest = (connection, request) =>
        {
            if (request is not (SearchRequest or StopRequest))
            {
                ScriptedHost.AnswerNormally(connection, request);
            }
        };
        using var deadline = new CancellationTokenSource();
        await using (var session = await pool.AcquireAsync(Token))
        {
            var search = session.SearchAsync(Limits, deadline.Token);
            await deadline.CancelAsync();
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => search);
            host.OnRequest = ScriptedHost.AnswerNormally;
        }

        await using var next = await pool.AcquireAsync(Token);

        Assert.Equal(2, host.Connections.Count);
        Assert.True(host.Connections[0].IsDisposed);
        Assert.Equal(ScriptedHost.SearchAnswer.BestMove, (await next.SearchAsync(Limits, Token)).BestMove);
    }

    [Fact]
    public async Task HostExit_FailsTheRequestAndRestartsTheHost()
    {
        var host = new ScriptedHost();
        await using var pool = await StartPoolAsync(host, workers: 1);
        host.OnRequest = (connection, request) =>
        {
            if (request is SearchRequest)
            {
                host.OnRequest = ScriptedHost.AnswerNormally;
                connection.Close();
            }
            else
            {
                ScriptedHost.AnswerNormally(connection, request);
            }
        };
        await using (var session = await pool.AcquireAsync(Token))
        {
            await Assert.ThrowsAsync<EngineFailureException>(() => session.SearchAsync(Limits, Token));
        }

        await using var next = await pool.AcquireAsync(Token);

        Assert.Equal(ScriptedHost.SearchAnswer.BestMove, (await next.SearchAsync(Limits, Token)).BestMove);
        Assert.Equal(2, host.Connections.Count);
        Assert.True(host.Connections[0].IsDisposed);
        Assert.Equal(1, pool.ReadyWorkerCount);
    }

    [Fact]
    public async Task HostError_FailsTheRequestWithoutRestartingTheHost()
    {
        var host = new ScriptedHost();
        await using var pool = await StartPoolAsync(host, workers: 1);
        host.OnRequest = (connection, request) =>
        {
            if (request is SearchRequest)
            {
                connection.Reply(new ErrorResponse("No legal moves."));
            }
            else
            {
                ScriptedHost.AnswerNormally(connection, request);
            }
        };
        await using var session = await pool.AcquireAsync(Token);

        var error = await Assert.ThrowsAsync<EngineFailureException>(() => session.SearchAsync(Limits, Token));

        Assert.Contains("No legal moves.", error.Message, StringComparison.Ordinal);
        await session.SetPositionAsync(Position.Initial, Token);
        Assert.Single(host.Connections);
        Assert.Equal(1, pool.ReadyWorkerCount);
    }

    [Fact]
    public async Task FailedRestart_LeavesTheWorkerNotReadyUntilTheNextAcquireStartsIt()
    {
        var host = new ScriptedHost();
        await using var pool = await StartPoolAsync(host, workers: 1);
        await FailHostAndItsRestartAsync(host, pool);

        Assert.Equal(0, pool.ReadyWorkerCount);
        host.FailToStart = false;
        host.OnRequest = ScriptedHost.AnswerNormally;

        await using var next = await pool.AcquireAsync(Token);

        Assert.Equal(1, pool.ReadyWorkerCount);
        Assert.Equal(ScriptedHost.SearchAnswer.BestMove, (await next.SearchAsync(Limits, Token)).BestMove);
    }

    [Fact]
    public async Task StartAfterFailedRestart_OutlivesTheAcquireThatStoppedWaitingForIt()
    {
        var host = new ScriptedHost();
        await using var pool = await StartPoolAsync(host, workers: 1);
        await FailHostAndItsRestartAsync(host, pool);
        host.FailToStart = false;
        host.OnRequest = (connection, request) =>
        {
            if (request is not SearchRequest)
            {
                ScriptedHost.AnswerNormally(connection, request);
            }
        };
        using var deadline = new CancellationTokenSource();

        var waiting = pool.AcquireAsync(deadline.Token);
        await deadline.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => waiting);
        var starting = host.Connections[^1];
        host.OnRequest = ScriptedHost.AnswerNormally;
        starting.Reply(ScriptedHost.SearchAnswer);
        await using var next = await pool.AcquireAsync(Token);
        Assert.Equal(2, host.Connections.Count);
        Assert.False(starting.IsDisposed);
        Assert.Equal(1, pool.ReadyWorkerCount);
    }

    /// <summary>Ends the worker's host during a search while new hosts fail to start.</summary>
    private static async Task FailHostAndItsRestartAsync(ScriptedHost host, KingsRowWorkerPool pool)
    {
        host.FailToStart = true;
        host.OnRequest = (connection, request) =>
        {
            if (request is SearchRequest)
            {
                connection.Close();
            }
            else
            {
                ScriptedHost.AnswerNormally(connection, request);
            }
        };
        await using var session = await pool.AcquireAsync(Token);
        await Assert.ThrowsAsync<EngineFailureException>(() => session.SearchAsync(Limits, Token));
    }

    private static async Task<KingsRowWorkerPool> StartPoolAsync(ScriptedHost host, int workers)
    {
        var pool = new KingsRowWorkerPool(
            host,
            Options.Create(new EngineOptions { Workers = workers }),
            NullLoggerFactory.Instance);
        await pool.StartAsync(Token);
        return pool;
    }
}

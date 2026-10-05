using Checkers.Application.Ports;
using Checkers.Domain;
using Checkers.Engine.Connections;
using Checkers.Engine.Protocol;
using Microsoft.Extensions.Logging;

namespace Checkers.Engine.Workers;

/// <summary>
/// One long-lived engine host, used by one session at a time. A host that fails is replaced; a host whose
/// request was cancelled is stopped, or replaced when it does not stop in time. A host start belongs to the
/// worker, not to the request that waits for it, so a request that gives up waiting does not abort the start.
/// </summary>
internal sealed class EngineWorker(int id, IHostConnectionFactory factory, ILogger logger) : IAsyncDisposable
{
    // Opening an 8-piece database on a cold disk can take seconds.
    private static readonly TimeSpan StartupTimeout = TimeSpan.FromSeconds(60);

    // KingsRow returns within 2 ms of a stop, so a host that takes longer is hung.
    private static readonly TimeSpan StopGrace = TimeSpan.FromMilliseconds(250);

    // The first search initializes KingsRow's endgame database (40-570 ms); warming up keeps that off requests.
    private static readonly SearchLimits WarmUpSearch = new(TimeSpan.FromMilliseconds(10), null);

    private readonly SemaphoreSlim _lock = new(1, 1);
    private readonly CancellationTokenSource _lifetime = new();
    private volatile IHostConnection? _connection;
    private Task? _start;
    private Task? _recovery;
    private bool _disposed;

    public bool IsReady => _connection is not null;

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await _lock.WaitAsync(cancellationToken);
        try
        {
            await EnsureConnectedAsync(cancellationToken);
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <summary>Waits for exclusive use of the worker, and for its host when an earlier start failed.</summary>
    public async Task<EngineSession> AcquireAsync(CancellationToken cancellationToken)
    {
        await _lock.WaitAsync(cancellationToken);
        try
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            await EnsureConnectedAsync(cancellationToken);
            return new EngineSession(this);
        }
        catch
        {
            _lock.Release();
            throw;
        }
    }

    /// <summary>Sends a request on behalf of the session that holds the worker and returns its response.</summary>
    /// <exception cref="EngineFailureException">The host failed or answered with an error.</exception>
    public async Task<TResponse> ExchangeAsync<TResponse>(HostRequest request, CancellationToken cancellationToken)
        where TResponse : HostResponse
    {
        var connection = _recovery is null ? _connection : null;
        if (connection is null)
        {
            throw new EngineFailureException("The engine worker is recovering from an earlier request of this session.");
        }

        try
        {
            return await RequestAsync<TResponse>(connection, request, cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            _recovery = StopSearchAsync(connection);
            throw;
        }
        catch (Exception exception) when (IsHostFailure(exception))
        {
            _recovery = RestartAsync(exception);
            throw new EngineFailureException($"The engine host failed: {exception.Message}", exception);
        }
    }

    /// <summary>Ends the current session; the next one starts once any recovery has finished.</summary>
    public void Release()
    {
        if (_recovery is { } recovery)
        {
            _recovery = null;
            _ = ReleaseAfterAsync(recovery);
        }
        else
        {
            _lock.Release();
        }
    }

    public async ValueTask DisposeAsync()
    {
        await _lock.WaitAsync();
        try
        {
            _disposed = true;
            await _lifetime.CancelAsync();
            if (_start is { } start)
            {
                await start.ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing);
            }

            if (_connection is { } connection)
            {
                _connection = null;
                await connection.DisposeAsync();
            }
        }
        finally
        {
            _lock.Release();
        }
    }

    private static async Task<TResponse> RequestAsync<TResponse>(
        IHostConnection connection,
        HostRequest request,
        CancellationToken cancellationToken)
        where TResponse : HostResponse
    {
        // The write is not cancellable: a partly written line would corrupt the stream, and it must be complete
        // before a stop request can follow it.
        await connection.SendAsync(request, CancellationToken.None);
        return await connection.ReceiveAsync(cancellationToken) switch
        {
            TResponse response => response,
            ErrorResponse error => throw new EngineFailureException($"The engine host reported: {error.Message}"),
            var other => throw new ProtocolException($"Expected {typeof(TResponse).Name} but received {other}."),
        };
    }

    private static bool IsHostFailure(Exception exception) => exception is IOException or ProtocolException;

    /// <summary>
    /// Waits until the worker has a host, starting one unless a start is already running. Called with the lock held.
    /// </summary>
    private Task EnsureConnectedAsync(CancellationToken cancellationToken)
    {
        if (_connection is not null)
        {
            return Task.CompletedTask;
        }

        if (_start is not { IsCompleted: false })
        {
            _start = ConnectAsync();
        }

        return _start.WaitAsync(cancellationToken);
    }

    private async Task ConnectAsync()
    {
        try
        {
            _connection = await StartHostWithinTimeoutAsync();
        }
        catch (EngineFailureException exception)
        {
            // Logged here because the request that started the host may no longer be waiting for it.
            EngineLog.WorkerStartFailed(logger, id, exception);
            throw;
        }
    }

    private async Task<IHostConnection> StartHostWithinTimeoutAsync()
    {
        using var startup = CancellationTokenSource.CreateLinkedTokenSource(_lifetime.Token);
        startup.CancelAfter(StartupTimeout);
        try
        {
            return await StartHostAsync(startup.Token);
        }
        catch (OperationCanceledException exception) when (!_lifetime.IsCancellationRequested)
        {
            throw new EngineFailureException($"The engine host did not start within {StartupTimeout}.", exception);
        }
        catch (Exception exception) when (IsHostFailure(exception))
        {
            throw new EngineFailureException($"The engine host failed to start: {exception.Message}", exception);
        }
    }

    private async Task<IHostConnection> StartHostAsync(CancellationToken cancellationToken)
    {
        var connection = await factory.ConnectAsync(cancellationToken);
        try
        {
            await RequestAsync<PositionSetResponse>(
                connection,
                new SetPositionRequest(Position.Initial.ToBoard()),
                cancellationToken);
            await RequestAsync<SearchResponse>(connection, WarmUpSearch.ToSearchRequest(), cancellationToken);
            return connection;
        }
        catch
        {
            await connection.DisposeAsync();
            throw;
        }
    }

    /// <summary>Stops the cancelled request's search and waits for its answer, after which the host is idle.</summary>
    private async Task StopSearchAsync(IHostConnection connection)
    {
        try
        {
            await connection.SendAsync(new StopRequest(), CancellationToken.None);
            using var grace = new CancellationTokenSource(StopGrace);
            await connection.ReceiveAsync(grace.Token);
        }
        catch (OperationCanceledException)
        {
            await RestartAsync(new TimeoutException($"The engine host did not stop within {StopGrace}."));
        }
        catch (Exception exception) when (IsHostFailure(exception))
        {
            await RestartAsync(exception);
        }
    }

    private async Task RestartAsync(Exception reason)
    {
        EngineLog.WorkerRestarting(logger, id, reason);
        if (_connection is { } broken)
        {
            _connection = null;
            await broken.DisposeAsync();
        }

        try
        {
            await EnsureConnectedAsync(CancellationToken.None);
        }
        catch (EngineFailureException)
        {
            // Already logged. The worker stays not ready; the next session that acquires it starts the host again.
        }
    }

    private async Task ReleaseAfterAsync(Task recovery)
    {
        try
        {
            await recovery;
        }
        finally
        {
            _lock.Release();
        }
    }
}

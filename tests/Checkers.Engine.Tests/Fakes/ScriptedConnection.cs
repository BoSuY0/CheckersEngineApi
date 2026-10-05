using System.Collections.Concurrent;
using System.Threading.Channels;
using Checkers.Engine.Connections;
using Checkers.Engine.Protocol;

namespace Checkers.Engine.Tests.Fakes;

/// <summary>An in-memory host whose answers are decided by the test.</summary>
internal sealed class ScriptedConnection(Action<ScriptedConnection, HostRequest> onRequest) : IHostConnection
{
    private readonly Channel<HostResponse> _responses = Channel.CreateUnbounded<HostResponse>();
    private readonly ConcurrentQueue<HostRequest> _requests = new();

    public IReadOnlyList<HostRequest> Requests => [.. _requests];

    public bool IsDisposed { get; private set; }

    public void Reply(HostResponse response) => _responses.Writer.TryWrite(response);

    /// <summary>Simulates a host that exited.</summary>
    public void Close() => _responses.Writer.TryComplete();

    public ValueTask SendAsync(HostRequest request, CancellationToken cancellationToken)
    {
        _requests.Enqueue(request);
        onRequest(this, request);
        return ValueTask.CompletedTask;
    }

    public async Task<HostResponse> ReceiveAsync(CancellationToken cancellationToken)
    {
        while (await _responses.Reader.WaitToReadAsync(cancellationToken))
        {
            if (_responses.Reader.TryRead(out var response))
            {
                return response;
            }
        }

        throw new EndOfStreamException();
    }

    public ValueTask DisposeAsync()
    {
        IsDisposed = true;
        Close();
        return ValueTask.CompletedTask;
    }
}

using Checkers.Engine.Protocol;

namespace Checkers.Engine.Connections;

/// <summary>
/// A running engine host that has reported ready. Transport failures surface as <see cref="IOException"/>
/// (<see cref="EndOfStreamException"/> once the host has exited) or <see cref="ProtocolException"/>.
/// Disposing it terminates the host.
/// </summary>
internal interface IHostConnection : IAsyncDisposable
{
    ValueTask SendAsync(HostRequest request, CancellationToken cancellationToken);

    /// <summary>
    /// Waits for the next response. Cancelling the wait does not lose that response: the next call returns it.
    /// </summary>
    Task<HostResponse> ReceiveAsync(CancellationToken cancellationToken);
}

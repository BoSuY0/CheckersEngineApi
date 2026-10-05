using Checkers.Application.Ports;

namespace Checkers.Engine.Connections;

internal interface IHostConnectionFactory
{
    /// <summary>
    /// Starts a new host and waits until it reports ready. A host that fails after its launch surfaces the
    /// transport failures described on <see cref="IHostConnection"/>.
    /// </summary>
    /// <exception cref="EngineFailureException">The host could not be launched.</exception>
    Task<IHostConnection> ConnectAsync(CancellationToken cancellationToken);
}

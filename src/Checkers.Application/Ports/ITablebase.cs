using Checkers.Domain;

namespace Checkers.Application.Ports;

/// <summary>Endgame database lookups.</summary>
public interface ITablebase
{
    /// <summary>
    /// Returns one value per position, from the side to move's point of view.
    /// Positions must be quiet (no capture for either side): the database holds no valid value otherwise.
    /// </summary>
    Task<IReadOnlyList<Wdl>> ProbeAsync(IReadOnlyList<Position> positions, CancellationToken cancellationToken);
}

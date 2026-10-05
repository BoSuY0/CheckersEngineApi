using Checkers.Domain;

namespace Checkers.Application.Ports;

/// <summary>The search side of one engine worker (the spec's EngineAdapter).</summary>
public interface IEngineAdapter
{
    Task SetPositionAsync(Position position, CancellationToken cancellationToken);

    /// <summary>Searches the position last set; <see cref="SearchLimits.MoveTime"/> is enforced by the engine.</summary>
    Task<SearchResult> SearchAsync(SearchLimits limits, CancellationToken cancellationToken);
}

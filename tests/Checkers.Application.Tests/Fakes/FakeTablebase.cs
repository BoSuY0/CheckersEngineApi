using Checkers.Application.Ports;
using Checkers.Domain;

namespace Checkers.Application.Tests.Fakes;

/// <summary>A database that knows the given canonical positions and fails the test when asked about a position with a capture.</summary>
internal sealed class FakeTablebase(IReadOnlyDictionary<string, Wdl> values) : ITablebase
{
    public FakeTablebase()
        : this(new Dictionary<string, Wdl>())
    {
    }

    public List<IReadOnlyList<Position>> Batches { get; } = [];

    public Task<IReadOnlyList<Wdl>> ProbeAsync(IReadOnlyList<Position> positions, CancellationToken cancellationToken)
    {
        Assert.All(positions, position =>
            Assert.False(position.HasCapture(PieceColor.Black) || position.HasCapture(PieceColor.White), $"{position} is not quiet."));
        Batches.Add(positions);
        return Task.FromResult<IReadOnlyList<Wdl>>(
            [.. positions.Select(position => values.GetValueOrDefault(position.ToString(), Wdl.Unknown))]);
    }
}

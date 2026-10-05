namespace Checkers.Domain;

/// <summary>A legal move, created only by the move generator of the position it belongs to.</summary>
public sealed class Move : IEquatable<Move>
{
    internal Move(IReadOnlyList<Square> path, IReadOnlyList<Square> captured)
    {
        Path = path;
        Captured = captured;
    }

    /// <summary>The start square followed by every landing square.</summary>
    public IReadOnlyList<Square> Path { get; }

    /// <summary>The squares of the captured pieces, in capture order.</summary>
    public IReadOnlyList<Square> Captured { get; }

    public Square From => Path[0];

    public Square To => Path[^1];

    public bool IsCapture => Captured.Count > 0;

    // The path determines the captured pieces, so it alone identifies the move.
    public bool Equals(Move? other) => other is not null && Path.SequenceEqual(other.Path);

    public override bool Equals(object? obj) => Equals(obj as Move);

    public override int GetHashCode()
    {
        var hash = new HashCode();
        foreach (var square in Path)
        {
            hash.Add(square);
        }

        return hash.ToHashCode();
    }

    /// <summary>PDN notation with the full path: "11-15" or "6x15x24".</summary>
    public override string ToString() => MoveNotation.Format(Path, IsCapture);
}

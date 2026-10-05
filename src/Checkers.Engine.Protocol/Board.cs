namespace Checkers.Engine.Protocol;

/// <summary>A position exchanged with the host: standard PDN square numbers (1-32) per piece type.</summary>
public sealed record Board(
    Side ToMove,
    IReadOnlyList<int> BlackMen,
    IReadOnlyList<int> BlackKings,
    IReadOnlyList<int> WhiteMen,
    IReadOnlyList<int> WhiteKings);

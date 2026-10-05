using Checkers.Application.Ports;

namespace Checkers.Application.Tests;

/// <summary>Positions shared by the tests, with the database values they need.</summary>
internal static class TestPositions
{
    /// <summary>The spec's sample: 16 pieces, Black to move, captures 14x23 or 16x23.</summary>
    public const string Midgame = "B:W18,19,22,25,27,28,30,32:B1,5,6,7,10,12,14,16";

    /// <summary>
    /// Four pieces, Black to move: 1-5 and 1-6 are quiet, 19-23 forces 27x18, and 19-24 lets White choose 27x20 or 28x19.
    /// </summary>
    public const string Endgame = "B:W27,28:BK1,19";

    /// <summary>
    /// Database values of the quiet positions below <see cref="Endgame"/>, each for its own side to move,
    /// that make 1-6 and 19-23 the winning moves for Black.
    /// </summary>
    public static IReadOnlyDictionary<string, Wdl> EndgameValues { get; } = new Dictionary<string, Wdl>
    {
        ["W:W27,28:BK5,19"] = Wdl.Draw,
        ["W:W27,28:BK6,19"] = Wdl.Loss,
        ["B:W18,28:BK1"] = Wdl.Win,
        ["B:W20,28:BK1"] = Wdl.Loss,
        ["B:W19,27:BK1"] = Wdl.Loss,
    };
}

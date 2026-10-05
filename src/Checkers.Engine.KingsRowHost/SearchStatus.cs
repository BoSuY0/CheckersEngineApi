using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text.RegularExpressions;
using Checkers.Engine.Protocol;

namespace Checkers.Engine.KingsRowHost;

/// <summary>A search status line written by KingsRow.</summary>
/// <param name="Value">Evaluation from Black's point of view: KingsRow always reports it that way.</param>
/// <param name="Depth">Nominal search depth.</param>
/// <param name="KiloNodesPerSecond">Search speed; KingsRow does not report a node count.</param>
/// <param name="Pv">The principal variation, start and end squares only for captures.</param>
/// <param name="IsRootDatabaseDraw">The root is an endgame database draw; KingsRow then lists drawing moves instead of searching.</param>
internal sealed partial record SearchStatus(
    int Value,
    int Depth,
    int KiloNodesPerSecond,
    IReadOnlyList<string> Pv,
    bool IsRootDatabaseDraw)
{
    /// <summary>KingsRow scores an endgame database draw as plus or minus this value.</summary>
    private const int DatabaseDrawValue = 1;

    /// <summary>KingsRow scores a known win or loss, such as an endgame database result, at least this high.</summary>
    private const int KnownResultValue = 2000;

    /// <summary>Parses a search result: <c>value=v,  depth d/a/m,  t.ts,  k kN/s,  pv m1 m2 ...</c>, or a root database draw: <c>depth d; m1* (p), ...</c>.</summary>
    /// <returns><see langword="false"/> for any other text, such as "Thinking...", "No moves" or the database initialisation notice.</returns>
    public static bool TryParse(string text, [NotNullWhen(true)] out SearchStatus? status)
    {
        if (Evaluated().Match(text) is { Success: true } evaluated)
        {
            status = new SearchStatus(
                Number(evaluated.Groups["value"]),
                Number(evaluated.Groups["depth"]),
                Number(evaluated.Groups["speed"]),
                evaluated.Groups["pv"].Value.Split(' ', StringSplitOptions.RemoveEmptyEntries),
                IsRootDatabaseDraw: false);
            return true;
        }

        if (DatabaseDraw().Match(text) is { Success: true } draw)
        {
            status = new SearchStatus(0, Number(draw.Groups["depth"]), 0, [], IsRootDatabaseDraw: true);
            return true;
        }

        status = null;
        return false;
    }

    public int ScoreFor(Side side) => side == Side.Black ? Value : -Value;

    public long NodesIn(TimeSpan elapsed) => (long)(KiloNodesPerSecond * 1000L * elapsed.TotalSeconds);

    /// <summary>Whether the result comes from the endgame database. KingsRow has no explicit flag for it.</summary>
    /// <param name="positionInDatabase">The position has no more pieces than the database covers.</param>
    public bool IsTablebaseHit(bool positionInDatabase) =>
        IsRootDatabaseDraw || (positionInDatabase && Math.Abs(Value) is DatabaseDrawValue or >= KnownResultValue);

    private static int Number(Group group) => int.Parse(group.Value, CultureInfo.InvariantCulture);

    [GeneratedRegex(@"^value[=<>](?<value>-?\d+), +depth (?<depth>\d+)/\S+, +\S+s, +(?<speed>\d+) kN/s,(?: +pv(?<pv>(?: +\S+)*))? *$", RegexOptions.CultureInvariant)]
    private static partial Regex Evaluated();

    [GeneratedRegex(@"^depth (?<depth>\d+);", RegexOptions.CultureInvariant)]
    private static partial Regex DatabaseDraw();
}

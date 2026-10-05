using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;

namespace Checkers.Domain;

/// <summary>
/// A parsed PDN move ("11-15", "6x24", "6x15x24") that is not yet known to be legal.
/// A capture may list only its start and end squares or its full path.
/// </summary>
public sealed partial class MoveNotation
{
    private MoveNotation(IReadOnlyList<Square> squares, bool isCapture)
    {
        Squares = squares;
        IsCapture = isCapture;
    }

    public IReadOnlyList<Square> Squares { get; }

    public bool IsCapture { get; }

    /// <exception cref="InvalidPdnException">The text is not a PDN move.</exception>
    public static MoveNotation Parse(string text) =>
        TryParse(text, out var notation)
            ? notation
            : throw new InvalidPdnException($"'{text}' is not a PDN move such as '11-15', '6x24' or '6x15x24'.");

    public static bool TryParse(string? text, [NotNullWhen(true)] out MoveNotation? notation)
    {
        notation = null;
        var trimmed = text?.Trim() ?? string.Empty;
        var match = Grammar().Match(trimmed);
        if (!match.Success)
        {
            return false;
        }

        var squares = new List<Square>();
        foreach (var digits in match.Groups["square"].Captures.Select(capture => capture.Value))
        {
            if (!PdnSquare.TryParse(digits, out var square))
            {
                return false;
            }

            squares.Add(square);
        }

        notation = new MoveNotation([.. squares], trimmed.Contains('x', StringComparison.Ordinal));
        return true;
    }

    /// <summary>
    /// True when the move has the same capture kind and start and end squares, and, if this notation
    /// lists intermediate squares, the same full path.
    /// </summary>
    public bool Matches(Move move)
    {
        ArgumentNullException.ThrowIfNull(move);
        return IsCapture == move.IsCapture
            && Squares[0] == move.From
            && Squares[^1] == move.To
            && (Squares.Count == 2 || Squares.SequenceEqual(move.Path));
    }

    public override string ToString() => Format(Squares, IsCapture);

    /// <summary>Writes squares joined by "x" for a capture and by "-" otherwise.</summary>
    internal static string Format(IEnumerable<Square> squares, bool isCapture) =>
        string.Join(isCapture ? "x" : "-", squares);

    [GeneratedRegex(
        $"^(?<square>{PdnSquare.Pattern})(?:-(?<square>{PdnSquare.Pattern})|(?:x(?<square>{PdnSquare.Pattern}))+)$",
        RegexOptions.CultureInvariant)]
    private static partial Regex Grammar();
}

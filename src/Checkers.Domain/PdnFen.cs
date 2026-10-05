using System.Text.RegularExpressions;

namespace Checkers.Domain;

/// <summary>
/// The PDN FEN position format, for example "B:W18,19,K22:B1-3,5": the side to move, then a White and a Black
/// piece list in either order. An item is an optional king prefix and a square or an inclusive ascending range.
/// </summary>
internal static partial class PdnFen
{
    private const string KingPrefix = "K";
    private const string Item = $"{KingPrefix}?{PdnSquare.Pattern}(?:-{PdnSquare.Pattern})?";
    private const string List = $@"[WB](?:{Item}(?:\s*,\s*{Item})*)?";

    /// <exception cref="InvalidPdnException">The text is malformed or the position is impossible.</exception>
    public static Position Parse(string pdn)
    {
        ArgumentNullException.ThrowIfNull(pdn);
        var match = Grammar().Match(pdn);
        if (!match.Success)
        {
            throw new InvalidPdnException($"'{pdn}' is not a PDN FEN position such as 'B:W21-32:B1-12'.");
        }

        var lists = match.Groups["list"].Captures;
        if (lists[0].Value[0] == lists[1].Value[0])
        {
            throw new InvalidPdnException($"'{pdn}' must have one White (W) and one Black (B) piece list.");
        }

        var pieces = new Dictionary<Square, Piece>();
        foreach (var (square, piece) in lists.SelectMany(list => ParseList(list.Value)))
        {
            if (!pieces.TryAdd(square, piece))
            {
                throw new InvalidPdnException($"Square {square} is listed more than once.");
            }
        }

        return Position.Create(ParseColor(match.Groups["side"].Value[0]), pieces);
    }

    /// <summary>The canonical form: side to move, White list, Black list, squares ascending, no ranges, no whitespace.</summary>
    public static string Format(Position position) =>
        $"{Letter(position.SideToMove)}:{FormatList(position, PieceColor.White)}:{FormatList(position, PieceColor.Black)}";

    private static IEnumerable<KeyValuePair<Square, Piece>> ParseList(string list)
    {
        var color = ParseColor(list[0]);
        foreach (var item in list[1..].Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))
        {
            var isKing = item.StartsWith(KingPrefix, StringComparison.Ordinal);
            var bounds = item[(isKing ? KingPrefix.Length : 0)..].Split('-');
            var first = ParseSquare(bounds[0]);
            var last = bounds.Length == 1 ? first : ParseSquare(bounds[1]);
            if (last.Number < first.Number)
            {
                throw new InvalidPdnException($"The range {item} is descending.");
            }

            var piece = new Piece(color, isKing ? PieceKind.King : PieceKind.Man);
            for (var number = first.Number; number <= last.Number; number++)
            {
                yield return new KeyValuePair<Square, Piece>(new Square(number), piece);
            }
        }
    }

    private static Square ParseSquare(string digits) =>
        PdnSquare.TryParse(digits, out var square)
            ? square
            : throw new InvalidPdnException($"Square {digits} is outside 1-{Square.Count}.");

    private static string FormatList(Position position, PieceColor color)
    {
        var items = Square.All
            .Select(square => (Square: square, Piece: position[square]))
            .Where(entry => entry.Piece?.Color == color)
            .Select(entry => entry.Piece?.Kind == PieceKind.King ? $"{KingPrefix}{entry.Square}" : $"{entry.Square}");
        return $"{Letter(color)}{string.Join(',', items)}";
    }

    private static PieceColor ParseColor(char letter) => letter == 'B' ? PieceColor.Black : PieceColor.White;

    private static char Letter(PieceColor color) => color == PieceColor.Black ? 'B' : 'W';

    [GeneratedRegex($@"^\s*(?<side>[WB])\s*:\s*(?<list>{List})\s*:\s*(?<list>{List})\s*$", RegexOptions.CultureInvariant)]
    private static partial Regex Grammar();
}

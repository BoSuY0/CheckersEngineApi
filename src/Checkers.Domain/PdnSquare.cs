using System.Globalization;

namespace Checkers.Domain;

/// <summary>A square number as written in PDN positions and moves.</summary>
internal static class PdnSquare
{
    /// <summary>The regular expression of a square number; <see cref="TryParse"/> checks its range.</summary>
    public const string Pattern = "[0-9]+";

    public static bool TryParse(string digits, out Square square)
    {
        square = default;
        return int.TryParse(digits, NumberStyles.None, CultureInfo.InvariantCulture, out var number)
            && Square.TryCreate(number, out square);
    }
}

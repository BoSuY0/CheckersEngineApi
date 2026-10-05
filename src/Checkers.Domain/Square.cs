using System.Globalization;

namespace Checkers.Domain;

/// <summary>A playable dark square in standard PDN numbering (1-32).</summary>
public readonly record struct Square
{
    public const int Count = 32;

    public Square(int number)
    {
        if (!IsValid(number))
        {
            throw new ArgumentOutOfRangeException(nameof(number), number, $"A square number is 1-{Count}.");
        }

        Number = number;
    }

    public static IReadOnlyList<Square> All { get; } =
        [.. Enumerable.Range(1, Count).Select(number => new Square(number))];

    public int Number { get; }

    public override string ToString() => Number.ToString(CultureInfo.InvariantCulture);

    internal static bool TryCreate(int number, out Square square)
    {
        var isValid = IsValid(number);
        square = isValid ? new Square(number) : default;
        return isValid;
    }

    private static bool IsValid(int number) => number is >= 1 and <= Count;
}

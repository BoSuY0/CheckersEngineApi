using Checkers.Domain;

namespace Checkers.Application.Tests;

internal static class MoveListExtensions
{
    public static string[] ToNotations(this IEnumerable<Move> moves) => [.. moves.Select(move => move.ToString())];
}

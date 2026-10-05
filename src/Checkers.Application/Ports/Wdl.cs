namespace Checkers.Application.Ports;

/// <summary>A game-theoretic value from the side to move's point of view.</summary>
public enum Wdl
{
    Unknown,
    Loss,
    Draw,
    Win,
}

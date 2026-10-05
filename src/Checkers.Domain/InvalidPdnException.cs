namespace Checkers.Domain;

/// <summary>A PDN position or move is malformed or describes an impossible position.</summary>
public sealed class InvalidPdnException(string message) : Exception(message);

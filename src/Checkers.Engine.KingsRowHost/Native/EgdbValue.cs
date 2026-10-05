namespace Checkers.Engine.KingsRowHost.Native;

/// <summary>The <c>LOOKUP_VALUE</c> results a Chinook WLD lookup can give. Every other value means unknown.</summary>
internal enum EgdbValue
{
    Unknown = 0,
    Win = 1,
    Loss = 2,
    Draw = 3,
}

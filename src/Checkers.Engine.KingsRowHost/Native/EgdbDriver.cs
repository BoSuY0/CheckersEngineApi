using System.Runtime.InteropServices;

namespace Checkers.Engine.KingsRowHost.Native;

/// <summary><c>EGDB_DRIVER</c>: an open database whose operations are function pointers. Only the ones used are declared.</summary>
[StructLayout(LayoutKind.Explicit)]
internal unsafe struct EgdbDriver
{
    /// <summary><c>lookup(handle, position, color, conditional)</c>: an <see cref="EgdbValue"/> for the side to move.</summary>
    [FieldOffset(0)]
    public delegate* unmanaged[Cdecl]<EgdbDriver*, EgdbBitboard*, EgdbColor, int, EgdbValue> Lookup;

    [FieldOffset(32)]
    public delegate* unmanaged[Cdecl]<EgdbDriver*, int> Close;
}

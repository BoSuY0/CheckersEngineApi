using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

namespace Checkers.Engine.KingsRowHost.Native;

/// <summary>egdb64.dll, the endgame database driver shipped with KingsRow.</summary>
internal sealed unsafe class EgdbLibrary
{
    /// <summary><c>EGDB_CHINOOK_WLD</c>, the database type of the Chinook win/loss/draw databases.</summary>
    public const int ChinookWld = 3;

    private const string FileName = "egdb64.dll";
    private const int Identified = 0;

    /// <summary><c>EGDB_NORMAL</c>: positions are passed as <see cref="EgdbBitboard"/>.</summary>
    private const int NormalBitboard = 0;

    private readonly delegate* unmanaged[Cdecl]<byte*, int*, int*, int> _identify;
    private readonly delegate* unmanaged[Cdecl]<int, int, int, byte*, delegate* unmanaged[Cdecl]<byte*, void>, EgdbDriver*> _open;

    private EgdbLibrary(nint handle)
    {
        _identify = (delegate* unmanaged[Cdecl]<byte*, int*, int*, int>)NativeLibrary.GetExport(handle, "egdb_identify");
        _open = (delegate* unmanaged[Cdecl]<int, int, int, byte*, delegate* unmanaged[Cdecl]<byte*, void>, EgdbDriver*>)
            NativeLibrary.GetExport(handle, "egdb_open");
    }

    public static EgdbLibrary Load(string engineDirectory) =>
        new(NativeLibrary.Load(Path.Combine(engineDirectory, FileName)));

    /// <summary>Finds the database in <paramref name="directory"/>.</summary>
    /// <returns><see langword="false"/> when the directory holds no database.</returns>
    public bool TryIdentify(string directory, out int type, out int maxPieces)
    {
        var path = AnsiStringMarshaller.ConvertToUnmanaged(directory);
        try
        {
            int identifiedType, identifiedPieces;
            var identified = _identify(path, &identifiedType, &identifiedPieces) == Identified;
            (type, maxPieces) = (identifiedType, identifiedPieces);
            return identified;
        }
        finally
        {
            AnsiStringMarshaller.Free(path);
        }
    }

    /// <returns>The open database, or <see langword="null"/> when it cannot be opened. The driver reports the reason on stderr.</returns>
    public EgdbDriver* Open(string directory, int pieces, int cacheMb)
    {
        var path = AnsiStringMarshaller.ConvertToUnmanaged(directory);
        try
        {
            return _open(NormalBitboard, pieces, cacheMb, path, &WriteMessage);
        }
        finally
        {
            AnsiStringMarshaller.Free(path);
        }
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static void WriteMessage(byte* message) => Console.Error.Write(AnsiStringMarshaller.ConvertToManaged(message));
}

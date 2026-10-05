using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

namespace Checkers.Engine.KingsRowHost.Native;

/// <summary>Kingsrow64.dll, the KingsRow English engine with the CheckerBoard engine interface.</summary>
internal sealed unsafe class KingsRowLibrary
{
    /// <summary>The size of the status and reply text buffers the engine writes to.</summary>
    public const int TextSize = 1024;

    private const int Handled = 1;

    /// <summary><c>CB_EXACT_TIME</c>: search for exactly the given time instead of treating it as an average.</summary>
    private const int ExactTime = 2;

    /// <summary><c>sizeof(CBmove)</c>.</summary>
    private const int MoveSize = 268;

    private readonly delegate* unmanaged<int*, int, double, byte*, int*, int, int, byte*, int> _getMove;
    private readonly delegate* unmanaged<byte*, byte*, int> _engineCommand;

    private KingsRowLibrary(nint handle)
    {
        _getMove = (delegate* unmanaged<int*, int, double, byte*, int*, int, int, byte*, int>)NativeLibrary.GetExport(handle, "getmove");
        _engineCommand = (delegate* unmanaged<byte*, byte*, int>)NativeLibrary.GetExport(handle, "enginecommand");
    }

    public static KingsRowLibrary Load(string engineDirectory) =>
        new(NativeLibrary.Load(Path.Combine(engineDirectory, "engines", "Kingsrow64.dll")));

    /// <summary>
    /// Searches <paramref name="board"/> for exactly <paramref name="seconds"/>, or until <paramref name="playNow"/> becomes
    /// nonzero, and plays the best move on <paramref name="board"/> in place. <paramref name="status"/> receives the result text.
    /// </summary>
    public void GetMove(int* board, int color, double seconds, byte* status, int* playNow)
    {
        // KingsRow reports its move only through the board and never fills CBmove, but it needs a valid buffer.
        var move = stackalloc byte[MoveSize];

        // The return value is ignored: KingsRow does not report win/loss/draw through it reliably.
        _getMove(board, color, seconds, status, playNow, ExactTime, 0, move);
    }

    /// <returns>The reply, or <see langword="null"/> when the engine does not recognise the command.</returns>
    public string? Command(string command)
    {
        var text = AnsiStringMarshaller.ConvertToUnmanaged(command);
        try
        {
            var reply = stackalloc byte[TextSize];
            return _engineCommand(text, reply) == Handled ? AnsiStringMarshaller.ConvertToManaged(reply) : null;
        }
        finally
        {
            AnsiStringMarshaller.Free(text);
        }
    }
}

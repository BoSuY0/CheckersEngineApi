using Checkers.Engine.KingsRowHost.Native;
using Checkers.Engine.Protocol;

namespace Checkers.Engine.KingsRowHost;

/// <summary>The Chinook win/loss/draw database, probed through egdb64.dll.</summary>
internal sealed unsafe class EndgameDatabase : IDisposable
{
    /// <summary>Read from disk when the position is not cached, instead of answering "not in cache".</summary>
    private const int Unconditional = 0;

    private EgdbDriver* _driver;

    private EndgameDatabase(EgdbDriver* driver, int pieces)
    {
        _driver = driver;
        Pieces = pieces;
    }

    /// <summary>The largest piece count the database covers; 0 when there is no database.</summary>
    public int Pieces { get; }

    /// <summary>Opens the database in <paramref name="directory"/>. A directory without a database gives an empty one.</summary>
    /// <exception cref="InvalidOperationException">The directory holds another kind of database, or it cannot be opened.</exception>
    public static EndgameDatabase Open(EgdbLibrary library, string directory)
    {
        if (!library.TryIdentify(directory, out var type, out var pieces))
        {
            return new EndgameDatabase(null, 0);
        }

        if (type != EgdbLibrary.ChinookWld)
        {
            throw new InvalidOperationException($"'{directory}' holds endgame database type {type}, not the Chinook WLD database.");
        }

        var driver = library.Open(directory, pieces, HostSettings.ProbeCacheMb);
        return driver is not null
            ? new EndgameDatabase(driver, pieces)
            : throw new InvalidOperationException($"Cannot open the {pieces}-piece endgame database in '{directory}'.");
    }

    /// <summary>Looks up a position in which neither side has a capture; the database has no valid value otherwise.</summary>
    public TablebaseValue Probe(Board board)
    {
        if (_driver is null || board.PieceCount() > Pieces)
        {
            return TablebaseValue.Unknown;
        }

        var position = EgdbBitboard.From(board);
        var color = board.ToMove == Side.Black ? EgdbColor.Black : EgdbColor.White;

        return _driver->Lookup(_driver, &position, color, Unconditional) switch
        {
            EgdbValue.Win => TablebaseValue.Win,
            EgdbValue.Loss => TablebaseValue.Loss,
            EgdbValue.Draw => TablebaseValue.Draw,
            _ => TablebaseValue.Unknown,
        };
    }

    public void Dispose()
    {
        if (_driver is not null)
        {
            _driver->Close(_driver);
            _driver = null;
        }
    }
}

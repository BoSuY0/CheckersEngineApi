using Checkers.Engine.KingsRowHost.Native;
using Checkers.Engine.Protocol;

namespace Checkers.Engine.KingsRowHost;

/// <summary>
/// One KingsRow worker: speaks the JSON-lines protocol on stdin and stdout, and writes diagnostics to stderr only.
/// Any failure ends the process with exit code 1; the API then restarts the worker.
/// </summary>
internal static class Program
{
    public static async Task<int> Main(string[] args)
    {
        try
        {
            var arguments = HostArguments.Parse(args);

            // KingsRow opens its opening book and evaluation weights relative to the current directory,
            // and crashes in its first search without them.
            Environment.CurrentDirectory = arguments.EngineDirectory;

            // Kingsrow64.dll imports egdb64.dll, which is not on the search path, so it is loaded first by full path.
            var egdb = EgdbLibrary.Load(arguments.EngineDirectory);
            var kingsRow = KingsRowLibrary.Load(arguments.EngineDirectory);

            using var database = EndgameDatabase.Open(egdb, arguments.DatabaseDirectory);
            var engine = KingsRowEngine.Start(kingsRow, arguments.DatabaseDirectory, database.Pieces);

            using var input = new StreamReader(Console.OpenStandardInput(), ProtocolChannels.Encoding);
            await using var output = new StreamWriter(Console.OpenStandardOutput(), ProtocolChannels.Encoding);
            var responses = ProtocolChannels.ResponseWriter(output);
            await responses.WriteAsync(new ReadyResponse(engine.Name, database.Pieces), CancellationToken.None);

            await new HostLoop(ProtocolChannels.RequestReader(input), responses, engine, database).RunAsync();
            return 0;
        }
        catch (Exception exception)
        {
            await Console.Error.WriteLineAsync(exception.ToString());
            return 1;
        }
    }
}

using Checkers.Engine.Connections;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Checkers.Engine.Tests.Connections;

public sealed class ProcessHostConnectionTests
{
    // A stand-in host that, like wine with its shared wineserver, leaves a detached process holding its error output.
    private const string HostScript = """
        (sleep 30 &)
        echo '{"type":"ready","engineName":"script","tablebasePieces":0}'
        cat > /dev/null
        """;

    private static CancellationToken Token => TestContext.Current.CancellationToken;

    [Fact]
    public async Task DisposeAsync_CompletesWhileADetachedProcessKeepsTheErrorOutputOpen()
    {
        Assert.SkipWhen(OperatingSystem.IsWindows(), "The stand-in host is a POSIX shell script.");
        var script = Path.GetTempFileName();
        try
        {
            await File.WriteAllTextAsync(script, HostScript, Token);
            var factory = new ProcessHostConnectionFactory(
                Options.Create(new EngineOptions
                {
                    Type = "chinook",
                    Path = script,
                    Workers = 1,
                    Databases = "unused",
                    HostPath = "/bin/sh",
                }),
                NullLoggerFactory.Instance);
            var connection = await factory.ConnectAsync(Token);

            await connection.DisposeAsync().AsTask().WaitAsync(TimeSpan.FromSeconds(10), Token);
        }
        finally
        {
            File.Delete(script);
        }
    }
}

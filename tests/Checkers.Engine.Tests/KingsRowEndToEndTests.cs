using System.Diagnostics;
using Checkers.Application.Caching;
using Checkers.Application.Moves;
using Checkers.Application.Strength;
using Checkers.Domain;
using Checkers.Engine.Connections;
using Checkers.Engine.Workers;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Checkers.Engine.Tests;

/// <summary>
/// The acceptance criteria against the real KingsRow host. Runs only when CHECKERS_KINGSROW_HOST names the host
/// executable; CHECKERS_KINGSROW_PATH and CHECKERS_KINGSROW_DATABASES then give the KingsRow and database
/// directories as Windows paths. Off Windows the host runs under wine, in the prefix that WINEPREFIX selects.
/// </summary>
public sealed class KingsRowEndToEndTests
{
    private static readonly string? HostPath = Environment.GetEnvironmentVariable("CHECKERS_KINGSROW_HOST");

    private static CancellationToken Token => TestContext.Current.CancellationToken;

    [Fact]
    public async Task StrongMidgamePosition_ReturnsALegalMoveWithin600Ms()
    {
        await using var pool = await StartPoolAsync();
        var service = CreateService(pool);
        const string Pdn = "B:W18,19,22,25,27,28,30,32:B1,5,6,7,10,12,14,16";

        var stopwatch = Stopwatch.StartNew();
        var suggestion = await service.SuggestAsync(Pdn, StrengthLevel.Strong, null, Token);
        stopwatch.Stop();

        Assert.Contains(suggestion.BestMove, Position.Parse(Pdn).LegalMoves);
        Assert.InRange(stopwatch.ElapsedMilliseconds, 0, 599);
    }

    [Fact]
    public async Task TablebasePosition_ReturnsATablebaseHitWithin50Ms()
    {
        await using var pool = await StartPoolAsync();
        var service = CreateService(pool);

        var stopwatch = Stopwatch.StartNew();
        var suggestion = await service.SuggestAsync("W:W25,26,27:B1,2", StrengthLevel.Weak, null, Token);
        stopwatch.Stop();

        Assert.True(suggestion.TablebaseHit);
        Assert.InRange(stopwatch.ElapsedMilliseconds, 0, 49);
    }

    private static async Task<KingsRowWorkerPool> StartPoolAsync()
    {
        Assert.SkipWhen(HostPath is null, "CHECKERS_KINGSROW_HOST is not set.");
        var options = Options.Create(new EngineOptions
        {
            Type = "chinook",
            Path = RequiredVariable("CHECKERS_KINGSROW_PATH"),
            Workers = 1,
            Databases = RequiredVariable("CHECKERS_KINGSROW_DATABASES"),
            HostPath = HostPath,
            Launcher = OperatingSystem.IsWindows() ? "" : "wine",
        });
        var pool = new KingsRowWorkerPool(
            new ProcessHostConnectionFactory(options, NullLoggerFactory.Instance),
            options,
            NullLoggerFactory.Instance);
        await pool.StartAsync(Token);
        return pool;
    }

    private static SuggestMoveService CreateService(KingsRowWorkerPool pool) =>
        new(
            pool,
            new StrengthPolicy(Options.Create(new LimitsOptions { DefaultSoftTimeMs = 300, DefaultHardTimeMs = 1200 })),
            Options.Create(new CacheOptions { Capacity = 100, TtlMinutes = 15 }),
            TimeProvider.System);

    private static string RequiredVariable(string name) =>
        Environment.GetEnvironmentVariable(name)
        ?? throw new InvalidOperationException($"{name} must be set when CHECKERS_KINGSROW_HOST is set.");
}

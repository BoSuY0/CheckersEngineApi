using System.Diagnostics;
using Checkers.Engine.Protocol;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Checkers.Engine.Connections;

/// <summary>Launches the configured host executable, through the launcher when one is set.</summary>
internal sealed class ProcessHostConnectionFactory(IOptions<EngineOptions> options, ILoggerFactory loggerFactory)
    : IHostConnectionFactory
{
    private readonly EngineOptions _options = options.Value;
    private readonly string _hostPath = Path.Combine(AppContext.BaseDirectory, options.Value.HostPath);
    private readonly ILogger _logger = loggerFactory.CreateLogger<ProcessHostConnection>();

    public Task<IHostConnection> ConnectAsync(CancellationToken cancellationToken) =>
        ProcessHostConnection.StartAsync(CreateStartInfo(), _logger, cancellationToken);

    private ProcessStartInfo CreateStartInfo()
    {
        var hasLauncher = _options.Launcher.Length > 0;
        var startInfo = new ProcessStartInfo(hasLauncher ? _options.Launcher : _hostPath)
        {
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            StandardInputEncoding = ProtocolChannels.Encoding,
            StandardOutputEncoding = ProtocolChannels.Encoding,
            StandardErrorEncoding = ProtocolChannels.Encoding,
        };

        if (hasLauncher)
        {
            startInfo.ArgumentList.Add(_hostPath);
        }

        foreach (var argument in new HostArguments(_options.Path, _options.Databases).ToArgs())
        {
            startInfo.ArgumentList.Add(argument);
        }

        return startInfo;
    }
}

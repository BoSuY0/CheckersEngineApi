using System.ComponentModel;
using System.Diagnostics;
using Checkers.Application.Ports;
using Checkers.Engine.Protocol;
using Microsoft.Extensions.Logging;

namespace Checkers.Engine.Connections;

/// <summary>An engine host process that speaks the protocol on its standard input and output.</summary>
internal sealed class ProcessHostConnection : IHostConnection
{
    private readonly Process _process;
    private readonly JsonLinesWriter<HostRequest> _requests;
    private readonly JsonLinesReader<HostResponse> _responses;
    private readonly CancellationTokenSource _diagnosticsStop = new();
    private readonly Task _diagnostics;
    private Task<HostResponse?>? _pendingResponse;

    private ProcessHostConnection(Process process, ILogger logger)
    {
        _process = process;
        _requests = ProtocolChannels.RequestWriter(process.StandardInput);
        _responses = ProtocolChannels.ResponseReader(process.StandardOutput);
        _diagnostics = LogDiagnosticsAsync(process.StandardError, process.Id, logger, _diagnosticsStop.Token);
    }

    public static async Task<IHostConnection> StartAsync(
        ProcessStartInfo startInfo,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        var connection = new ProcessHostConnection(Launch(startInfo), logger);
        try
        {
            var first = await connection.ReceiveAsync(cancellationToken);
            if (first is not ReadyResponse ready)
            {
                throw new ProtocolException($"Expected a ready message but received {first}.");
            }

            EngineLog.HostStarted(logger, connection._process.Id, ready.EngineName, ready.TablebasePieces);
            return connection;
        }
        catch
        {
            await connection.DisposeAsync();
            throw;
        }
    }

    public ValueTask SendAsync(HostRequest request, CancellationToken cancellationToken) =>
        _requests.WriteAsync(request, cancellationToken);

    public async Task<HostResponse> ReceiveAsync(CancellationToken cancellationToken)
    {
        // Cancelling a pending line read would leave the reader in an undefined state, so a cancelled wait
        // leaves the read running and the next call continues it.
        _pendingResponse ??= _responses.ReadAsync(CancellationToken.None).AsTask();
        var response = await _pendingResponse.WaitAsync(cancellationToken);
        _pendingResponse = null;
        return response ?? throw new EndOfStreamException("The engine host closed its output.");
    }

    public async ValueTask DisposeAsync()
    {
        // With a launcher such as wine, the engine may run in a child of the started process.
        _process.Kill(entireProcessTree: true);
        await _process.WaitForExitAsync();

        // Processes the host started can keep its error output open after it exited: under wine, the shared
        // wineserver inherits it. So the end of that output cannot mark the end of the diagnostics.
        await _diagnosticsStop.CancelAsync();
        await _diagnostics;
        _diagnosticsStop.Dispose();
        _process.Dispose();
    }

    private static Process Launch(ProcessStartInfo startInfo)
    {
        var failure = $"Cannot start the engine host '{startInfo.FileName}'.";
        try
        {
            return Process.Start(startInfo) ?? throw new EngineFailureException(failure);
        }
        catch (Win32Exception exception)
        {
            throw new EngineFailureException(failure, exception);
        }
    }

    private static async Task LogDiagnosticsAsync(
        StreamReader errors,
        int processId,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        try
        {
            while (await errors.ReadLineAsync(cancellationToken) is { } line)
            {
                EngineLog.HostDiagnostic(logger, processId, line);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // The connection was disposed; see DisposeAsync.
        }
    }
}

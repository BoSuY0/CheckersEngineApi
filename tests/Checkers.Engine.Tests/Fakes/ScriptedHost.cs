using System.Collections.Concurrent;
using Checkers.Application.Ports;
using Checkers.Engine.Connections;
using Checkers.Engine.Protocol;

namespace Checkers.Engine.Tests.Fakes;

/// <summary>Starts <see cref="ScriptedConnection"/>s that answer like a healthy host unless told otherwise.</summary>
internal sealed class ScriptedHost : IHostConnectionFactory
{
    public static readonly SearchResponse SearchAnswer = new("11-15", ["11-15", "23-19"], 12, 4200, 10, false);

    private readonly ConcurrentQueue<ScriptedConnection> _connections = new();

    /// <summary>Decides how every connection, including running ones, answers a request.</summary>
    public Action<ScriptedConnection, HostRequest> OnRequest { get; set; } = AnswerNormally;

    public bool FailToStart { get; set; }

    public IReadOnlyList<ScriptedConnection> Connections => [.. _connections];

    public static void AnswerNormally(ScriptedConnection connection, HostRequest request)
    {
        HostResponse? response = request switch
        {
            SetPositionRequest => new PositionSetResponse(),
            SearchRequest => SearchAnswer,
            ProbeRequest probe => new ProbeResponse([.. probe.Boards.Select(_ => TablebaseValue.Win)]),
            _ => null,
        };

        if (response is not null)
        {
            connection.Reply(response);
        }
    }

    public Task<IHostConnection> ConnectAsync(CancellationToken cancellationToken)
    {
        if (FailToStart)
        {
            return Task.FromException<IHostConnection>(new EngineFailureException("The scripted host failed to start."));
        }

        var connection = new ScriptedConnection((target, request) => OnRequest(target, request));
        _connections.Enqueue(connection);
        return Task.FromResult<IHostConnection>(connection);
    }
}

namespace Checkers.Application.Ports;

/// <summary>The fixed set of long-lived engine workers.</summary>
public interface IEngineWorkerPool
{
    /// <summary>The engine name reported in every suggestion.</summary>
    string EngineName { get; }

    int WorkerCount { get; }

    int ReadyWorkerCount { get; }

    /// <summary>Waits for the next worker in round-robin order.</summary>
    /// <exception cref="EngineFailureException">The worker's engine could not be started.</exception>
    Task<IEngineSession> AcquireAsync(CancellationToken cancellationToken);
}

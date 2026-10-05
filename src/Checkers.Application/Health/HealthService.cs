using Checkers.Application.Ports;

namespace Checkers.Application.Health;

public sealed class HealthService(IEngineWorkerPool workers)
{
    public HealthStatus GetStatus()
    {
        var ready = workers.ReadyWorkerCount;
        return new HealthStatus(ready == workers.WorkerCount, ready);
    }
}

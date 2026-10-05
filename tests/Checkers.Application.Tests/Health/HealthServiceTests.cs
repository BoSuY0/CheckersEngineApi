using Checkers.Application.Health;
using Checkers.Application.Tests.Fakes;

namespace Checkers.Application.Tests.Health;

public sealed class HealthServiceTests
{
    [Fact]
    public void GetStatus_AllWorkersReady_IsOk() =>
        Assert.Equal(
            new HealthStatus(Ok: true, Workers: 2),
            new HealthService(new FakeEnginePool { WorkerCount = 2, ReadyWorkerCount = 2 }).GetStatus());

    [Fact]
    public void GetStatus_WorkerNotReady_IsNotOkAndCountsReadyWorkers() =>
        Assert.Equal(
            new HealthStatus(Ok: false, Workers: 1),
            new HealthService(new FakeEnginePool { WorkerCount = 2, ReadyWorkerCount = 1 }).GetStatus());
}

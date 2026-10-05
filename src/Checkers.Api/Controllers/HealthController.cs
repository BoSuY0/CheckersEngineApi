using Checkers.Application.Health;
using Microsoft.AspNetCore.Mvc;

namespace Checkers.Api.Controllers;

[ApiController]
[Route("healthz")]
public sealed class HealthController(HealthService health) : ControllerBase
{
    [HttpGet]
    public HealthStatus Get() => health.GetStatus();
}

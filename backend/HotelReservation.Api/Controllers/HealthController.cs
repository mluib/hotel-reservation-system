using Microsoft.AspNetCore.Mvc;

namespace HotelReservation.Api.Controllers;

/// <summary>
/// Unauthenticated liveness check for the frontend: it has nothing to do with app
/// health/metrics tooling, it exists purely so the frontend can tell "backend
/// unreachable" (network/connection error) apart from "backend reachable but the
/// request itself failed" (e.g. 401 on a bad login) and show the right thing for each.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class HealthController : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult Get() => Ok(new { status = "ok" });
}

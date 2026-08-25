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
    /// <summary>
    /// Returns 200 if the backend process is up. No auth, no dependency checks (e.g.
    /// the database) -- see the class summary for why.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult Get() => Ok(new { status = "ok" });
}

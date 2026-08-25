using HotelReservation.Application.DTOs;
using HotelReservation.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HotelReservation.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AccountController : ControllerBase
{
    private readonly IAuthService _auth;

    public AccountController(IAuthService auth)
    {
        _auth = auth;
    }

    /// <summary>
    /// Creates a new Identity login and its linked Customer profile in one step, then
    /// signs the new account in immediately.
    /// </summary>
    [HttpPost("register")]
    [AllowAnonymous]
    [ProducesResponseType<AuthenticationResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Register(RegisterRequest request)
    {
        var resp = await _auth.RegisterAsync(request);
        return StatusCode(StatusCodes.Status201Created, resp);
    }

    /// <summary>
    /// Authenticates an existing login and returns a JWT for subsequent requests.
    /// </summary>
    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType<AuthenticationResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login(LoginRequest request)
    {
        var resp = await _auth.LoginAsync(request);
        return Ok(resp);
    }
}

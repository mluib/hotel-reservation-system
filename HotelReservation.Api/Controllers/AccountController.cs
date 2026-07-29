using HotelReservation.Application.Authentication;
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

    [HttpPost("register")]
    public async Task<IActionResult> Register(RegisterRequest request)
    {
        try
        {
            var resp = await _auth.RegisterAsync(request);
            return Ok(resp);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginRequest request)
    {
        try
        {
            var resp = await _auth.LoginAsync(request);
            return Ok(resp);
        }
        catch (InvalidOperationException)
        {
            return Unauthorized();
        }
    }
}

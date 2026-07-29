using HotelReservation.Application.Authentication;
using Microsoft.AspNetCore.Identity;

namespace HotelReservation.Infrastructure.Services;

public class AuthService : IAuthService
{
    private readonly UserManager<IdentityUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly IJwtTokenService _jwt;

    public AuthService(
        UserManager<IdentityUser> userManager,
        RoleManager<IdentityRole> roleManager,
        IJwtTokenService jwt)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _jwt = jwt;
    }

    public async Task<AuthenticationResponse> RegisterAsync(RegisterRequest request)
    {
        var user = new IdentityUser { UserName = request.Email, Email = request.Email };
        var result = await _userManager.CreateAsync(user, request.Password);
        if (!result.Succeeded)
        {
            throw new InvalidOperationException(string.Join(';', result.Errors.Select(e => e.Description)));
        }

        var role = string.IsNullOrWhiteSpace(request.Role) ? "User" : request.Role;
        if (!await _roleManager.RoleExistsAsync(role))
            await _roleManager.CreateAsync(new IdentityRole(role));

        await _userManager.AddToRoleAsync(user, role);

        var roles = await _userManager.GetRolesAsync(user);
        var token = _jwt.GenerateToken(user.Id, user.UserName ?? string.Empty, roles);

        return new AuthenticationResponse { Token = token };
    }

    public async Task<AuthenticationResponse> LoginAsync(LoginRequest request)
    {
        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user == null) throw new InvalidOperationException("Invalid credentials.");

        var valid = await _userManager.CheckPasswordAsync(user, request.Password);
        if (!valid) throw new InvalidOperationException("Invalid credentials.");

        var roles = await _userManager.GetRolesAsync(user);
        var token = _jwt.GenerateToken(user.Id, user.UserName ?? string.Empty, roles);

        return new AuthenticationResponse { Token = token };
    }
}

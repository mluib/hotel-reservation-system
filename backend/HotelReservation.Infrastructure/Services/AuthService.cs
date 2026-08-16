using HotelReservation.Application.DTOs;
using HotelReservation.Application.Interfaces;
using HotelReservation.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

namespace HotelReservation.Infrastructure.Services;

public class AuthService : IAuthService
{
    private readonly UserManager<IdentityUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly IJwtTokenService _jwt;
    private readonly ICustomerRepository _customerRepository;
    private readonly ILogger<AuthService> _logger;

    public AuthService(
        UserManager<IdentityUser> userManager,
        RoleManager<IdentityRole> roleManager,
        IJwtTokenService jwt,
        ICustomerRepository customerRepository,
        ILogger<AuthService> logger)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _jwt = jwt;
        _customerRepository = customerRepository;
        _logger = logger;
    }

    public async Task<AuthenticationResponse> RegisterAsync(RegisterRequest request)
    {
        var user = new IdentityUser { UserName = request.Email, Email = request.Email };
        var result = await _userManager.CreateAsync(user, request.Password);
        if (!result.Succeeded)
        {
            // Never log the submitted password -- only Identity's own validation messages.
            _logger.LogWarning(
                "Registration failed for {Email}: {Errors}",
                request.Email, string.Join(';', result.Errors.Select(e => e.Description)));
            throw new InvalidOperationException(string.Join(';', result.Errors.Select(e => e.Description)));
        }

        // Always assign the "Customer" role
        var role = "Customer";

        if (!await _roleManager.RoleExistsAsync(role))
            await _roleManager.CreateAsync(new IdentityRole(role));

        await _userManager.AddToRoleAsync(user, role);

        // Create a domain Customer and link it to the Identity user id
        var customer = new Customer(request.FirstName, request.LastName, request.Email, user.Id);
        await _customerRepository.AddAsync(customer);

        var roles = await _userManager.GetRolesAsync(user);
        var token = _jwt.GenerateToken(user.Id, user.UserName ?? string.Empty, roles);

        _logger.LogInformation("Customer registered: {Email}", request.Email);

        return new AuthenticationResponse { Token = token };
    }

    public async Task<AuthenticationResponse> LoginAsync(LoginRequest request)
    {
        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user == null)
        {
            _logger.LogWarning("Login failed for {Email}: no such account", request.Email);
            throw new InvalidOperationException("Invalid credentials.");
        }

        var valid = await _userManager.CheckPasswordAsync(user, request.Password);
        if (!valid)
        {
            _logger.LogWarning("Login failed for {Email}: wrong password", request.Email);
            throw new InvalidOperationException("Invalid credentials.");
        }

        var roles = await _userManager.GetRolesAsync(user);
        var token = _jwt.GenerateToken(user.Id, user.UserName ?? string.Empty, roles);

        _logger.LogInformation("Login succeeded for {Email}", request.Email);

        return new AuthenticationResponse { Token = token };
    }
}

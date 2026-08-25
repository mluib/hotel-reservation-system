using HotelReservation.Application.Common.Exceptions;
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

            var message = string.Join(';', result.Errors.Select(e => e.Description));

            // Identity's own error codes tell us whether this is "someone already has this
            // email" (a real conflict) versus a request-shape problem (weak password, etc.).
            var isDuplicate = result.Errors.Any(e => e.Code is "DuplicateUserName" or "DuplicateEmail");
            if (isDuplicate)
                throw new ConflictException(message);

            throw new ValidationException(message);
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
        // Auth-enumeration avoidance
        // "No such account" and "wrong password" both throw the same generic message
        // below (and get the same logged detail only server-side). Distinguishing them
        // in the response would let a caller enumerate which emails are registered.
        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user == null)
        {
            _logger.LogWarning("Login failed for {Email}: no such account", request.Email);
            throw new UnauthenticatedException("Invalid credentials.");
        }

        var valid = await _userManager.CheckPasswordAsync(user, request.Password);
        if (!valid)
        {
            _logger.LogWarning("Login failed for {Email}: wrong password", request.Email);
            throw new UnauthenticatedException("Invalid credentials.");
        }

        var roles = await _userManager.GetRolesAsync(user);

        // Every Customer-role Identity user is expected to have a linked domain Customer
        // (RegisterAsync creates both together). A Customer-role login with no such record
        // means something removed the Customer row without also removing this login --
        // e.g. deleting the row directly instead of through DeleteCustomer, or (before this
        // check existed) DeleteCustomer itself leaving the login behind. Rejecting here,
        // the same as a wrong password, is safer and clearer than letting the token issue
        // and having every later "my profile"/"my reservations" call 404 instead.
        if (roles.Contains("Customer"))
        {
            var customer = await _customerRepository.GetByIdentityUserIdAsync(user.Id);
            if (customer == null)
            {
                _logger.LogWarning("Login failed for {Email}: Customer role but no linked customer profile", request.Email);
                throw new UnauthenticatedException("Invalid credentials.");
            }
        }

        var token = _jwt.GenerateToken(user.Id, user.UserName ?? string.Empty, roles);

        _logger.LogInformation("Login succeeded for {Email}", request.Email);

        return new AuthenticationResponse { Token = token };
    }

    public async Task DeleteUserAsync(string identityUserId)
    {
        var user = await _userManager.FindByIdAsync(identityUserId);
        if (user == null)
            return;

        await _userManager.DeleteAsync(user);
    }
}

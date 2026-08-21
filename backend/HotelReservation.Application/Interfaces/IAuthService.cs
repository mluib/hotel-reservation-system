using HotelReservation.Application.DTOs;

namespace HotelReservation.Application.Interfaces;

public interface IAuthService
{
    Task<AuthenticationResponse> RegisterAsync(RegisterRequest request);

    Task<AuthenticationResponse> LoginAsync(LoginRequest request);

    /// <summary>
    /// Deletes the Identity login (account, not domain data) for the given Identity user
    /// id. Used by <c>DeleteCustomer</c> so removing a customer also revokes their ability
    /// to log in -- otherwise the account would still authenticate with no linked
    /// <see cref="HotelReservation.Domain.Entities.Customer"/> profile behind it. A no-op if no such
    /// Identity user exists.
    /// </summary>
    Task DeleteUserAsync(string identityUserId);
}

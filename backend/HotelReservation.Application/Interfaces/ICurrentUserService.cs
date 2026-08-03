namespace HotelReservation.Application.Interfaces;

public interface ICurrentUserService
{
    /// <summary>
    /// The current authenticated user's id (from JWT claims), or null when unauthenticated.
    /// </summary>
    string? UserId { get; }

    /// <summary>
    /// Returns true when the current user is in the specified role.
    /// </summary>
    bool IsInRole(string role);
}

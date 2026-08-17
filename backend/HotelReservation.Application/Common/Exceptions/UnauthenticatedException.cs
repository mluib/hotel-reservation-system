namespace HotelReservation.Application.Common.Exceptions;

/// <summary>
/// Defensive check for an unauthenticated caller reaching a use case that requires an
/// identity. In practice these sites are already unreachable because the controller-level
/// <c>[Authorize]</c> attribute blocks anonymous callers first -- this exists as a second
/// line of defense, not the primary gate. Maps to 401.
/// </summary>
public sealed class UnauthenticatedException : AppException
{
    public UnauthenticatedException(string message) : base(message) { }
}

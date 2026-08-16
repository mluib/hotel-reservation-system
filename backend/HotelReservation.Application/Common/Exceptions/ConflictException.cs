namespace HotelReservation.Application.Common.Exceptions;

/// <summary>
/// The request conflicts with existing state (double-booking, deleting a room/customer
/// that still has reservations, registering an email that's already taken). Maps to 409.
/// </summary>
public sealed class ConflictException : AppException
{
    public ConflictException(string message) : base(message) { }
}

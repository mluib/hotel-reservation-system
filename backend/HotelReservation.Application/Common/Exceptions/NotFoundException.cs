namespace HotelReservation.Application.Common.Exceptions;

/// <summary>
/// The requested resource (room, customer, reservation, hotel, ...) doesn't exist. Maps to 404.
/// </summary>
public sealed class NotFoundException : AppException
{
    public NotFoundException(string message) : base(message) { }
}

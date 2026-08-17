using HotelReservation.Domain.Enums;

namespace HotelReservation.Application.DTOs;

public class ReservationDto
{
    public Guid Id { get; set; }

    public Guid RoomId { get; set; }

    public Guid CustomerId { get; set; }

    public DateTime CheckIn { get; set; }

    public DateTime CheckOut { get; set; }

    public ReservationStatus Status { get; set; }

    public decimal PricePerNight { get; set; }
}

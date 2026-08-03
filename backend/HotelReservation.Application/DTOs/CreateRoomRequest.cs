using HotelReservation.Domain.Enums;

namespace HotelReservation.Application.DTOs;

public class CreateRoomRequest
{
    public string Number { get; set; }

    public RoomType Type { get; set; }

    public decimal PricePerNight { get; set; }

    public Guid HotelId { get; set; }
}

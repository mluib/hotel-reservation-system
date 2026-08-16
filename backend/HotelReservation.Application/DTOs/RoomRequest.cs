using HotelReservation.Domain.Enums;

namespace HotelReservation.Application.DTOs;

// Shared by both create and update: the two were previously separate types
// (CreateRoomRequest/UpdateRoomRequest) with an identical shape, since a room's
// number/type/price/hotel are the same fields whether you're creating or editing one.
public class RoomRequest
{
    public string Number { get; set; }

    public RoomType Type { get; set; }

    public decimal PricePerNight { get; set; }

    public Guid HotelId { get; set; }
}

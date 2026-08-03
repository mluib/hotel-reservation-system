using HotelReservation.Domain.Enums;

namespace HotelReservation.Application.DTOs;

public class RoomFilterRequest
{
    public RoomType? Type { get; set; }

    public decimal? MinPrice { get; set; }

    public decimal? MaxPrice { get; set; }

    public DateTime? CheckIn { get; set; }

    public DateTime? CheckOut { get; set; }
}

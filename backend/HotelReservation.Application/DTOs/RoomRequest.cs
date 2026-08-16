using System.ComponentModel.DataAnnotations;
using HotelReservation.Domain.Enums;

namespace HotelReservation.Application.DTOs;

public class RoomRequest
{
    [Required]
    [MaxLength(20)]
    public string Number { get; set; }

    public RoomType Type { get; set; }

    [Range(0.01, double.MaxValue, ErrorMessage = "PricePerNight must be greater than zero.")]
    public decimal PricePerNight { get; set; }

    public Guid HotelId { get; set; }
}

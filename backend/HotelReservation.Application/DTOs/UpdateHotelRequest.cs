using System.ComponentModel.DataAnnotations;

namespace HotelReservation.Application.DTOs;

public class UpdateHotelRequest
{
    [Required]
    public string Name { get; set; }

    [Required]
    public string Address { get; set; }
}

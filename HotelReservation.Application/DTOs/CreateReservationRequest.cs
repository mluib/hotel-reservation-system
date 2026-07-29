using System;
using System.Collections.Generic;
using System.Text;

namespace HotelReservation.Application.DTOs;

public class CreateReservationRequest
{
    public Guid RoomId { get; set; }

    public DateTime CheckIn { get; set; }

    public DateTime CheckOut { get; set; }
}
using System;
using System.Collections.Generic;
using System.Text;

using HotelReservation.Domain.Entities;

namespace HotelReservation.Application.Interfaces;

public interface IReservationRepository
{
    Task AddAsync(Reservation reservation);

    Task<bool> HasOverlappingReservationAsync(
        Guid roomId,
        DateTime checkIn,
        DateTime checkOut);
}
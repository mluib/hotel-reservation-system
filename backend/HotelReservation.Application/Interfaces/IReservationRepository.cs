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

    Task<bool> RoomExistsAsync(Guid roomId);

    Task<bool> CustomerExistsAsync(Guid customerId);

    Task<bool> ExistsForRoomAsync(Guid roomId);

    Task<bool> ExistsForCustomerAsync(Guid customerId);

    Task<Reservation?> GetByIdAsync(Guid id);

    Task<IEnumerable<Reservation>> GetAllAsync();

    Task<IEnumerable<Reservation>> GetByCustomerIdAsync(Guid customerId);

    Task UpdateAsync(Reservation reservation);

    Task DeleteAsync(Reservation reservation);
}
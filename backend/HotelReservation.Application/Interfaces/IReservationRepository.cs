using System;
using System.Collections.Generic;
using System.Text;

using HotelReservation.Domain.Entities;
using HotelReservation.Domain.ValueObjects;

namespace HotelReservation.Application.Interfaces;

public interface IReservationRepository
{
    Task AddAsync(Reservation reservation);

    Task<bool> HasOverlappingReservationAsync(
        Guid roomId,
        DateTime checkIn,
        DateTime checkOut);

    /// <summary>
    /// Ids of every room with a non-cancelled reservation overlapping <paramref name="range"/>.
    /// </summary>
    /// <remarks>
    /// Lets <c>RoomRepository</c>'s availability filter go through this repository instead of
    /// querying the <c>Reservations</c> table directly -- that direct reach-across was a
    /// layering smell (a repository nominally responsible for Rooms querying a different
    /// aggregate's table itself). The tradeoff: this is now two round trips instead of one
    /// correlated subquery (this call, then <c>RoomRepository</c>'s own query using the
    /// result via <c>.Contains()</c>), accepted for the cleaner repository boundary.
    /// </remarks>
    Task<IEnumerable<Guid>> GetOverlappingRoomIdsAsync(DateRange range);

    Task<bool> ExistsForRoomAsync(Guid roomId);

    Task<bool> ExistsForCustomerAsync(Guid customerId);

    Task<Reservation?> GetByIdAsync(Guid id);

    Task<IEnumerable<Reservation>> GetAllAsync();

    Task<IEnumerable<Reservation>> GetByCustomerIdAsync(Guid customerId);

    Task UpdateAsync(Reservation reservation);

    Task DeleteAsync(Reservation reservation);
}
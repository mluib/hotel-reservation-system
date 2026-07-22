using HotelReservation.Application.Interfaces;
using HotelReservation.Domain.Entities;
using HotelReservation.Domain.Enums;
using HotelReservation.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace HotelReservation.Infrastructure.Repositories;

public class ReservationRepository : IReservationRepository
{
    private readonly HotelDbContext _context;

    public ReservationRepository(
        HotelDbContext context)
    {
        _context = context;
    }


    public async Task AddAsync(
        Reservation reservation)
    {
        await _context.Reservations.AddAsync(reservation);

        await _context.SaveChangesAsync();
    }


    public async Task<bool> HasOverlappingReservationAsync(
        Guid roomId,
        DateTime checkIn,
        DateTime checkOut)
    {
        return await _context.Reservations
            .AnyAsync(r =>
                r.RoomId == roomId &&
                r.Status != ReservationStatus.Cancelled &&
                r.CheckIn < checkOut &&
                r.CheckOut > checkIn);
    }
}
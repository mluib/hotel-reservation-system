using HotelReservation.Application.Interfaces;
using HotelReservation.Domain.Entities;
using HotelReservation.Domain.Enums;
using HotelReservation.Domain.ValueObjects;
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
                r.Stay.CheckIn < checkOut &&
                r.Stay.CheckOut > checkIn);
    }

    public async Task<IEnumerable<Guid>> GetOverlappingRoomIdsAsync(DateRange range)
    {
        return await _context.Reservations
            .Where(r =>
                r.Status != ReservationStatus.Cancelled &&
                r.Stay.CheckIn < range.CheckOut &&
                range.CheckIn < r.Stay.CheckOut)
            .Select(r => r.RoomId)
            .ToListAsync();
    }

    // Intentionally status-agnostic (no filter on Status): even a cancelled reservation
    // is still a historical record referencing this room, and would be orphaned by the delete.
    public async Task<bool> ExistsForRoomAsync(Guid roomId)
    {
        return await _context.Reservations.AnyAsync(r => r.RoomId == roomId);
    }

    // Same reasoning as ExistsForRoomAsync above: any reservation, any status, blocks delete.
    public async Task<bool> ExistsForCustomerAsync(Guid customerId)
    {
        return await _context.Reservations.AnyAsync(r => r.CustomerId == customerId);
    }

    public async Task<Reservation?> GetByIdAsync(Guid id)
    {
        return await _context.Reservations
            .FirstOrDefaultAsync(r => r.Id == id);
    }

    public async Task<IEnumerable<Reservation>> GetAllAsync()
    {
        return await _context.Reservations
            .ToListAsync();
    }

    public async Task<IEnumerable<Reservation>> GetByCustomerIdAsync(Guid customerId)
    {
        return await _context.Reservations
            .Where(r => r.CustomerId == customerId)
            .ToListAsync();
    }

    public async Task UpdateAsync(Reservation reservation)
    {
        _context.Reservations.Update(reservation);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(Reservation reservation)
    {
        _context.Reservations.Remove(reservation);
        await _context.SaveChangesAsync();
    }
}
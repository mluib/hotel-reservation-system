using HotelReservation.Application.DTOs;
using HotelReservation.Application.Interfaces;
using HotelReservation.Domain.Entities;
using HotelReservation.Domain.Enums;
using HotelReservation.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace HotelReservation.Infrastructure.Repositories;

public class RoomRepository : IRoomRepository
{
    private readonly HotelDbContext _context;

    public RoomRepository(HotelDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(Room room)
    {
        await _context.Rooms.AddAsync(room);
        await _context.SaveChangesAsync();
    }

    public async Task<Room?> GetByIdAsync(Guid id)
    {
        return await _context.Rooms
            .FirstOrDefaultAsync(r => r.Id == id);
    }

    public async Task<IEnumerable<Room>> GetAllAsync(RoomFilterRequest? filter = null)
    {
        var query = _context.Rooms.AsQueryable();

        if (filter?.Type != null)
            query = query.Where(r => r.Type == filter.Type);

        if (filter?.MinPrice != null)
            query = query.Where(r => r.PricePerNight.Amount >= filter.MinPrice);

        if (filter?.MaxPrice != null)
            query = query.Where(r => r.PricePerNight.Amount <= filter.MaxPrice);

        if (filter?.CheckIn != null && filter?.CheckOut != null)
        {
            // Room no longer has a Reservations navigation (Phase 6 aggregate cleanup), so
            // this is now a subquery against Reservations directly rather than filtering an
            // already-loaded collection -- still translates to a single SQL query (a
            // correlated NOT EXISTS), just expressed differently.
            var checkIn = filter.CheckIn.Value;
            var checkOut = filter.CheckOut.Value;
            var overlappingRoomIds = _context.Reservations
                .Where(res =>
                    res.Status != ReservationStatus.Cancelled &&
                    res.Stay.CheckIn < checkOut &&
                    checkIn < res.Stay.CheckOut)
                .Select(res => res.RoomId);

            query = query.Where(r => !overlappingRoomIds.Contains(r.Id));
        }

        return await query.ToListAsync();
    }

    public async Task UpdateAsync(Room room)
    {
        _context.Rooms.Update(room);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(Room room)
    {
        _context.Rooms.Remove(room);
        await _context.SaveChangesAsync();
    }
}

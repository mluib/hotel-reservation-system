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
            .Include(r => r.Reservations)
            .FirstOrDefaultAsync(r => r.Id == id);
    }

    public async Task<IEnumerable<Room>> GetAllAsync(RoomFilterRequest? filter = null)
    {
        var query = _context.Rooms
            .Include(r => r.Reservations)
            .AsQueryable();

        if (filter?.Type != null)
            query = query.Where(r => r.Type == filter.Type);

        if (filter?.MinPrice != null)
            query = query.Where(r => r.PricePerNight >= filter.MinPrice);

        if (filter?.MaxPrice != null)
            query = query.Where(r => r.PricePerNight <= filter.MaxPrice);

        if (filter?.CheckIn != null && filter?.CheckOut != null)
        {
            var checkIn = filter.CheckIn.Value;
            var checkOut = filter.CheckOut.Value;
            query = query.Where(r => !r.Reservations.Any(res =>
                res.Status != ReservationStatus.Cancelled &&
                res.CheckIn < checkOut &&
                checkIn < res.CheckOut));
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

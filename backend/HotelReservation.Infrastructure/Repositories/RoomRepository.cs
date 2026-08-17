using HotelReservation.Application.DTOs;
using HotelReservation.Application.Interfaces;
using HotelReservation.Domain.Entities;
using HotelReservation.Domain.Enums;
using HotelReservation.Domain.ValueObjects;
using HotelReservation.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace HotelReservation.Infrastructure.Repositories;

public class RoomRepository : IRoomRepository
{
    private readonly HotelDbContext _context;
    private readonly IReservationRepository _reservationRepository;

    public RoomRepository(HotelDbContext context, IReservationRepository reservationRepository)
    {
        _context = context;
        _reservationRepository = reservationRepository;
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
            // Goes through IReservationRepository rather than querying _context.Reservations
            // directly -- Room no longer has a Reservations navigation to filter (Phase 6
            // aggregate cleanup), and reaching into another aggregate's table directly from
            // here would be a layering smell regardless. Two round trips instead of one
            // correlated subquery; see GetOverlappingRoomIdsAsync's own remarks for why
            // that's an accepted tradeoff.
            var range = new DateRange(filter.CheckIn.Value, filter.CheckOut.Value);
            var overlappingRoomIds = await _reservationRepository.GetOverlappingRoomIdsAsync(range);
 
            // alternative with one correlated subquery (one sql query) instead of two sql queries
            //var checkIn = filter.CheckIn.Value;
            //var checkOut = filter.CheckOut.Value;
            //var overlappingRoomIds = _context.Reservations
            //    .Where(res =>
            //        res.Status != ReservationStatus.Cancelled &&
            //        res.Stay.CheckIn < checkOut &&
            //        checkIn < res.Stay.CheckOut)
            //    .Select(res => res.RoomId);

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

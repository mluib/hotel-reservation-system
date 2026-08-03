using HotelReservation.Application.Interfaces;
using HotelReservation.Domain.Entities;
using HotelReservation.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace HotelReservation.Infrastructure.Repositories;

public class HotelRepository : IHotelRepository
{
    private readonly HotelDbContext _context;

    public HotelRepository(HotelDbContext context)
    {
        _context = context;
    }

    public async Task<Hotel?> GetAsync()
    {
        // Assume single hotel in system; return first
        return await _context.Hotels
            .Include(h => h.Rooms)
            .FirstOrDefaultAsync();
    }

    public async Task UpdateAsync(Hotel hotel)
    {
        _context.Hotels.Update(hotel);
        await _context.SaveChangesAsync();
    }
}

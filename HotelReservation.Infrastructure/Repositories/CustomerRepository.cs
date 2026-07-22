using HotelReservation.Application.Interfaces;
using HotelReservation.Domain.Entities;
using HotelReservation.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace HotelReservation.Infrastructure.Repositories;

public class CustomerRepository : ICustomerRepository
{
    private readonly HotelDbContext _context;

    public CustomerRepository(HotelDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(Customer customer)
    {
        await _context.Customers.AddAsync(customer);
        await _context.SaveChangesAsync();
    }

    public async Task<Customer?> GetByIdAsync(Guid id)
    {
        return await _context.Customers
            .Include(c => c.Reservations)
            .FirstOrDefaultAsync(c => c.Id == id);
    }

    public async Task<IEnumerable<Customer>> GetAllAsync()
    {
        return await _context.Customers
            .Include(c => c.Reservations)
            .ToListAsync();
    }

    public async Task UpdateAsync(Customer customer)
    {
        _context.Customers.Update(customer);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(Customer customer)
    {
        _context.Customers.Remove(customer);
        await _context.SaveChangesAsync();
    }
}

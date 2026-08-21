using System;
using System.Threading.Tasks;
using FluentAssertions;
using HotelReservation.Application.Interfaces;
using HotelReservation.Domain.Entities;
using HotelReservation.Domain.Enums;
using HotelReservation.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace HotelReservation.Tests.Integration.Repositories;

public class CustomerRepositoryTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public CustomerRepositoryTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task DeleteAsync_CustomerWithReservation_ThrowsDbUpdateException()
    {
        Guid customerId;
        using (var seedScope = _factory.Services.CreateScope())
        {
            var db = seedScope.ServiceProvider.GetRequiredService<HotelDbContext>();

            var hotel = await db.Hotels.FirstOrDefaultAsync();
            if (hotel == null)
            {
                hotel = new Hotel("Test Hotel", "1 Test St");
                db.Hotels.Add(hotel);
                await db.SaveChangesAsync();
            }
            var room = new Room("Test Room", "R-" + Guid.NewGuid().ToString("N")[..8], RoomType.Single, 100m, hotel.Id);
            var customer = new Customer("Jane", "Doe", $"jane-{Guid.NewGuid():N}@example.com");
            db.Rooms.Add(room);
            db.Customers.Add(customer);
            await db.SaveChangesAsync();

            // Reservation.RoomId is also a Restrict foreign key -- needs a real Room, same
            // reasoning as ReservationRepositoryTests' seeding helper.
            db.Reservations.Add(new Reservation(room.Id, customer.Id,
                DateTime.UtcNow.AddDays(1), DateTime.UtcNow.AddDays(2), 100m));
            await db.SaveChangesAsync();
            customerId = customer.Id;
        }

        // Fresh scope for the delete attempt -- see RoomRepositoryTests' equivalent test for
        // why: reusing the seeding context would let EF's own change-tracker fixup logic
        // catch this client-side (InvalidOperationException) instead of genuinely exercising
        // the database-level Restrict constraint (DbUpdateException).
        using var deleteScope = _factory.Services.CreateScope();
        var customers = deleteScope.ServiceProvider.GetRequiredService<ICustomerRepository>();
        var reloaded = await customers.GetByIdAsync(customerId);
        await customers.Invoking(c => c.DeleteAsync(reloaded!))
            .Should().ThrowAsync<DbUpdateException>();
    }

    [Fact]
    public async Task AddAsync_ThenGetByIdAsync_RoundTripsEmailAddress()
    {
        using var scope = _factory.Services.CreateScope();
        var customers = scope.ServiceProvider.GetRequiredService<ICustomerRepository>();

        var email = $"round-trip-{Guid.NewGuid():N}@example.com";
        var customer = new Customer("Alex", "Smith", email);

        await customers.AddAsync(customer);

        var reloaded = await customers.GetByIdAsync(customer.Id);

        reloaded.Should().NotBeNull();
        reloaded!.Email.Value.Should().Be(email);
    }
}

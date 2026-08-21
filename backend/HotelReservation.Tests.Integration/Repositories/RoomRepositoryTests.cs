using System;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using HotelReservation.Application.DTOs;
using HotelReservation.Application.Interfaces;
using HotelReservation.Domain.Entities;
using HotelReservation.Domain.Enums;
using HotelReservation.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace HotelReservation.Tests.Integration.Repositories;

// Resolved via the factory's DI container (not `new RoomRepository(...)` directly) so
// these exercise the real, configured HotelDbContext -- OnModelCreating, the Money/DateRange
// owned-entity mappings, and the Restrict foreign keys from Phase 6 -- against an actual
// database provider (SQLite here), not mocks.
public class RoomRepositoryTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public RoomRepositoryTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetAllAsync_DateFilter_ExcludesRoomWithOverlappingConfirmedReservation()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<HotelDbContext>();
        var rooms = scope.ServiceProvider.GetRequiredService<IRoomRepository>();

        var hotel = await SeedHotelAsync(db);
        var customer = await SeedCustomerAsync(db);
        var busyRoom = new Room("Test Room", "R-" + Guid.NewGuid().ToString("N")[..8], RoomType.Single, 100m, hotel.Id);
        var freeRoom = new Room("Test Room", "R-" + Guid.NewGuid().ToString("N")[..8], RoomType.Single, 100m, hotel.Id);
        db.Rooms.AddRange(busyRoom, freeRoom);
        await db.SaveChangesAsync();

        db.Reservations.Add(new Reservation(busyRoom.Id, customer.Id,
            new DateTime(2028, 3, 10), new DateTime(2028, 3, 15), 100m));
        await db.SaveChangesAsync();

        var filter = new RoomFilterRequest
        {
            CheckIn = new DateTime(2028, 3, 12),
            CheckOut = new DateTime(2028, 3, 13)
        };

        var result = (await rooms.GetAllAsync(filter)).ToList();

        result.Should().Contain(r => r.Id == freeRoom.Id);
        result.Should().NotContain(r => r.Id == busyRoom.Id);
    }

    [Fact]
    public async Task GetAllAsync_DateFilter_IncludesRoomWithOnlyCancelledOverlap()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<HotelDbContext>();
        var rooms = scope.ServiceProvider.GetRequiredService<IRoomRepository>();

        var hotel = await SeedHotelAsync(db);
        var customer = await SeedCustomerAsync(db);
        var room = new Room("Test Room", "R-" + Guid.NewGuid().ToString("N")[..8], RoomType.Single, 100m, hotel.Id);
        db.Rooms.Add(room);
        await db.SaveChangesAsync();

        var reservation = new Reservation(room.Id, customer.Id,
            new DateTime(2028, 4, 10), new DateTime(2028, 4, 15), 100m);
        reservation.Cancel();
        db.Reservations.Add(reservation);
        await db.SaveChangesAsync();

        var filter = new RoomFilterRequest
        {
            CheckIn = new DateTime(2028, 4, 12),
            CheckOut = new DateTime(2028, 4, 13)
        };

        var result = (await rooms.GetAllAsync(filter)).ToList();

        result.Should().Contain(r => r.Id == room.Id);
    }

    [Fact]
    public async Task DeleteAsync_RoomWithReservation_ThrowsDbUpdateException()
    {
        Guid roomId;
        using (var seedScope = _factory.Services.CreateScope())
        {
            var db = seedScope.ServiceProvider.GetRequiredService<HotelDbContext>();
            var hotel = await SeedHotelAsync(db);
            var customer = await SeedCustomerAsync(db);
            var room = new Room("Test Room", "R-" + Guid.NewGuid().ToString("N")[..8], RoomType.Single, 100m, hotel.Id);
            db.Rooms.Add(room);
            await db.SaveChangesAsync();

            db.Reservations.Add(new Reservation(room.Id, customer.Id,
                DateTime.UtcNow.AddDays(1), DateTime.UtcNow.AddDays(2), 100m));
            await db.SaveChangesAsync();
            roomId = room.Id;
        }

        // A fresh scope/DbContext for the delete attempt, deliberately not the one used to
        // seed the reservation above: with both entities tracked in the same context, EF's
        // own change-tracker "fixup" logic catches the severed Restrict relationship
        // client-side (InvalidOperationException) before any SQL is even sent, which would
        // prove the C# object graph is consistent but say nothing about the database itself.
        // A fresh context has no in-memory knowledge of the relationship, so this genuinely
        // exercises the database-level constraint from Phase 6's aggregate cleanup, not just
        // EF's own bookkeeping -- matching a real request, which likewise never loads
        // Reservations when deleting a Room (Room.Reservations no longer exists).
        using var deleteScope = _factory.Services.CreateScope();
        var rooms = deleteScope.ServiceProvider.GetRequiredService<IRoomRepository>();
        var reloadedRoom = await rooms.GetByIdAsync(roomId);
        await rooms.Invoking(r => r.DeleteAsync(reloadedRoom!))
            .Should().ThrowAsync<DbUpdateException>();
    }

    [Fact]
    public async Task AddAsync_ThenGetByIdAsync_RoundTripsMoney()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<HotelDbContext>();
        var rooms = scope.ServiceProvider.GetRequiredService<IRoomRepository>();

        var hotel = await SeedHotelAsync(db);
        var room = new Room("Test Room", "R-" + Guid.NewGuid().ToString("N")[..8], RoomType.Double, 219.95m, hotel.Id);

        await rooms.AddAsync(room);

        var reloaded = await rooms.GetByIdAsync(room.Id);

        reloaded.Should().NotBeNull();
        reloaded!.PricePerNight.Amount.Should().Be(219.95m);
        reloaded.PricePerNight.Currency.Should().Be("EUR");
    }

    private static async Task<Hotel> SeedHotelAsync(HotelDbContext db)
    {
        var existing = await db.Hotels.FirstOrDefaultAsync();
        if (existing != null) return existing;

        var hotel = new Hotel("Test Hotel", "1 Test St");
        db.Hotels.Add(hotel);
        await db.SaveChangesAsync();
        return hotel;
    }

    // Reservation.CustomerId is a Restrict foreign key (Phase 6 aggregate cleanup) -- a
    // Reservation referencing a non-existent Customer id is rejected by the database, so
    // every test that inserts a Reservation needs a real Customer row first.
    private static async Task<Customer> SeedCustomerAsync(HotelDbContext db)
    {
        var customer = new Customer("Test", "Customer", $"test-{Guid.NewGuid():N}@example.com");
        db.Customers.Add(customer);
        await db.SaveChangesAsync();
        return customer;
    }
}

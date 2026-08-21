using System;
using System.Threading.Tasks;
using FluentAssertions;
using HotelReservation.Application.Interfaces;
using HotelReservation.Domain.Entities;
using HotelReservation.Domain.Enums;
using HotelReservation.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Xunit;
using Microsoft.Extensions.DependencyInjection;

namespace HotelReservation.Tests.Integration.Repositories;

public class ReservationRepositoryTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public ReservationRepositoryTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task HasOverlappingReservationAsync_OverlappingConfirmedReservation_ReturnsTrue()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<HotelDbContext>();
        var reservations = scope.ServiceProvider.GetRequiredService<IReservationRepository>();

        var (roomId, customerId) = await SeedRoomAndCustomerAsync(db);
        await reservations.AddAsync(new Reservation(roomId, customerId,
            new DateTime(2028, 5, 1), new DateTime(2028, 5, 10), 100m));

        var result = await reservations.HasOverlappingReservationAsync(
            roomId, new DateTime(2028, 5, 5), new DateTime(2028, 5, 15));

        result.Should().BeTrue();
    }

    [Fact]
    public async Task HasOverlappingReservationAsync_AdjacentNotOverlapping_ReturnsFalse()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<HotelDbContext>();
        var reservations = scope.ServiceProvider.GetRequiredService<IReservationRepository>();

        var (roomId, customerId) = await SeedRoomAndCustomerAsync(db);
        // Existing stay ends exactly when the queried range begins -- back-to-back, not
        // an overlap (same boundary rule as DateRange.Overlaps()).
        await reservations.AddAsync(new Reservation(roomId, customerId,
            new DateTime(2028, 6, 1), new DateTime(2028, 6, 10), 100m));

        var result = await reservations.HasOverlappingReservationAsync(
            roomId, new DateTime(2028, 6, 10), new DateTime(2028, 6, 15));

        result.Should().BeFalse();
    }

    [Fact]
    public async Task HasOverlappingReservationAsync_OnlyCancelledOverlap_ReturnsFalse()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<HotelDbContext>();
        var reservations = scope.ServiceProvider.GetRequiredService<IReservationRepository>();

        var (roomId, customerId) = await SeedRoomAndCustomerAsync(db);
        var reservation = new Reservation(roomId, customerId,
            new DateTime(2028, 7, 1), new DateTime(2028, 7, 10), 100m);
        reservation.Cancel();
        await reservations.AddAsync(reservation);

        var result = await reservations.HasOverlappingReservationAsync(
            roomId, new DateTime(2028, 7, 5), new DateTime(2028, 7, 8));

        result.Should().BeFalse();
    }

    [Fact]
    public async Task AddAsync_ThenGetByIdAsync_RoundTripsDateRangeAndMoney()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<HotelDbContext>();
        var reservations = scope.ServiceProvider.GetRequiredService<IReservationRepository>();

        var (roomId, customerId) = await SeedRoomAndCustomerAsync(db);
        var checkIn = new DateTime(2028, 8, 1, 0, 0, 0, DateTimeKind.Utc);
        var checkOut = new DateTime(2028, 8, 6, 0, 0, 0, DateTimeKind.Utc);
        var reservation = new Reservation(roomId, customerId, checkIn, checkOut, 175.50m);

        await reservations.AddAsync(reservation);

        var reloaded = await reservations.GetByIdAsync(reservation.Id);

        reloaded.Should().NotBeNull();
        reloaded!.Stay.CheckIn.Should().Be(checkIn);
        reloaded.Stay.CheckOut.Should().Be(checkOut);
        reloaded.PricePerNight.Amount.Should().Be(175.50m);
        reloaded.PricePerNight.Currency.Should().Be("EUR");
    }

    // Reservation.RoomId/CustomerId are both Restrict foreign keys (Phase 6 aggregate
    // cleanup) -- a Reservation referencing ids that don't correspond to real rows is
    // rejected by the database, so every test here needs a real Room and Customer first.
    private static async Task<(Guid RoomId, Guid CustomerId)> SeedRoomAndCustomerAsync(HotelDbContext db)
    {
        var hotel = await db.Hotels.FirstOrDefaultAsync();
        if (hotel == null)
        {
            hotel = new Hotel("Test Hotel", "1 Test St");
            db.Hotels.Add(hotel);
            await db.SaveChangesAsync();
        }

        var room = new Room("Test Room", "R-" + Guid.NewGuid().ToString("N")[..8], RoomType.Single, 100m, hotel.Id);
        var customer = new Customer("Test", "Customer", $"test-{Guid.NewGuid():N}@example.com");
        db.Rooms.Add(room);
        db.Customers.Add(customer);
        await db.SaveChangesAsync();

        return (room.Id, customer.Id);
    }
}

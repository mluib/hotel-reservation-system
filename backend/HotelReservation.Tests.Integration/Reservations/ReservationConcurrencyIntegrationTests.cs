using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Threading.Tasks;
using FluentAssertions;
using HotelReservation.Application.DTOs;
using HotelReservation.Domain.Entities;
using HotelReservation.Domain.Enums;
using HotelReservation.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace HotelReservation.Tests.Integration.Reservations;

public class ReservationConcurrencyIntegrationTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public ReservationConcurrencyIntegrationTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    // The concurrency test for the double-booking race fixed in CreateReservation (see
    // docs/decisions.md): fires two create-requests for the exact same room/dates
    // together, not one after the other, so they're genuinely racing to pass the
    // overlap check before either commits. Asserts the database's own atomicity
    // guarantee -- not just the application-level HasOverlappingReservationAsync check
    // alone, which a purely in-process race could slip past -- is what prevents both
    // from succeeding.
    [Fact]
    public async Task ConcurrentCreate_SameRoomAndDates_OnlyOneSucceeds()
    {
        var roomId = await SeedRoomAsync();

        var clientA = await RegisterAndLoginAsync($"racer-a-{Guid.NewGuid()}@example.com");
        var clientB = await RegisterAndLoginAsync($"racer-b-{Guid.NewGuid()}@example.com");

        var checkIn = DateTime.UtcNow.Date.AddDays(30);
        var checkOut = checkIn.AddDays(2);
        var request = new { RoomId = roomId, CheckIn = checkIn, CheckOut = checkOut };

        var taskA = clientA.PostAsJsonAsync("/api/reservations", request);
        var taskB = clientB.PostAsJsonAsync("/api/reservations", request);
        await Task.WhenAll(taskA, taskB);

        var statusCodes = new[] { taskA.Result.StatusCode, taskB.Result.StatusCode };

        statusCodes.Should().Contain(HttpStatusCode.Created,
            "one of the two concurrent requests should have won the race and booked the room");
        statusCodes.Should().Contain(HttpStatusCode.Conflict,
            "the other should have been rejected as a double-booking, not silently allowed through");
    }

    // Seeds a Hotel (if none exists yet) + Room directly via DbContext, same approach as
    // AuthorizationIntegrationTests' own seeding helper -- bypasses the admin-only
    // create-room endpoint, which isn't what this test is about.
    private async Task<Guid> SeedRoomAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<HotelDbContext>();

        var hotel = await db.Hotels.FirstOrDefaultAsync();
        if (hotel == null)
        {
            hotel = new Hotel("Test Hotel", "1 Test St");
            db.Hotels.Add(hotel);
            await db.SaveChangesAsync();
        }

        var room = new Room("Concurrency Test Room", Guid.NewGuid().ToString("N")[..8], RoomType.Single, 100m, hotel.Id);
        db.Rooms.Add(room);
        await db.SaveChangesAsync();

        return room.Id;
    }

    private async Task<HttpClient> RegisterAndLoginAsync(string email)
    {
        var client = _factory.CreateClient();
        var register = new { Email = email, Password = "P@ssw0rd!", FirstName = "Racer", LastName = "Test" };
        var response = await client.PostAsJsonAsync("/api/account/register", register);
        response.EnsureSuccessStatusCode();

        var body = await response.Content.ReadFromJsonAsync<AuthenticationResponse>();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", body!.Token);
        return client;
    }
}

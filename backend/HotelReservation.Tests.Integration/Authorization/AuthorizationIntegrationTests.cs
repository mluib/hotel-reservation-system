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
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace HotelReservation.Tests.Integration.Authorization;

public class AuthorizationIntegrationTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public AuthorizationIntegrationTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Post_Reservations_Anonymous_Returns401()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/reservations", new
        {
            RoomId = Guid.NewGuid(),
            CheckIn = DateTime.UtcNow.AddDays(1),
            CheckOut = DateTime.UtcNow.AddDays(2)
        });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Get_Reservations_CustomerRole_Returns403()
    {
        var client = await RegisterAndLoginAsync($"customer-{Guid.NewGuid()}@example.com");

        var response = await client.GetAsync("/api/reservations");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Get_Reservations_AdminRole_Returns200()
    {
        // Confirms the 403 above is genuine role-based rejection, not the endpoint being
        // broken for everyone -- an Admin token hitting the same endpoint should succeed.
        var client = await SeedAdminAndLoginAsync();

        var response = await client.GetAsync("/api/reservations");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Cancel_AnotherCustomersReservation_Returns403()
    {
        var ownerClient = await RegisterAndLoginAsync($"owner-{Guid.NewGuid()}@example.com");
        var strangerClient = await RegisterAndLoginAsync($"stranger-{Guid.NewGuid()}@example.com");

        var reservationId = await SeedRoomAndReservationForCurrentOwnerAsync(ownerClient);

        var response = await strangerClient.PostAsync($"/api/reservations/{reservationId}/cancel", null);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    // -- helpers --

    // Registration always assigns the "Customer" role (see AuthService.RegisterAsync), so
    // this is the shared path for every Customer-role client these tests need.
    private async Task<HttpClient> RegisterAndLoginAsync(string email)
    {
        var client = _factory.CreateClient();
        var register = new { Email = email, Password = "P@ssw0rd!", FirstName = "Test", LastName = "User" };
        var response = await client.PostAsJsonAsync("/api/account/register", register);
        response.EnsureSuccessStatusCode();

        var body = await response.Content.ReadFromJsonAsync<AuthenticationResponse>();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", body!.Token);
        return client;
    }

    // Register can't produce an Admin token (it always assigns "Customer"), so this seeds
    // the role/user directly via UserManager/RoleManager -- mirroring Program.cs's own
    // SeedDevAdminAsync -- then logs in through the real endpoint rather than minting a
    // token directly, so this still exercises the actual login code path.
    private async Task<HttpClient> SeedAdminAndLoginAsync()
    {
        var email = $"admin-{Guid.NewGuid()}@example.com";
        const string password = "P@ssw0rd!";

        using (var scope = _factory.Services.CreateScope())
        {
            var services = scope.ServiceProvider;

            var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
            if (!await roleManager.RoleExistsAsync("Admin"))
                await roleManager.CreateAsync(new IdentityRole("Admin"));

            var userManager = services.GetRequiredService<UserManager<IdentityUser>>();
            var user = new IdentityUser { UserName = email, Email = email };
            var createResult = await userManager.CreateAsync(user, password);
            createResult.Succeeded.Should().BeTrue();
            await userManager.AddToRoleAsync(user, "Admin");
        }

        var client = _factory.CreateClient();
        var loginResponse = await client.PostAsJsonAsync("/api/account/login", new { Email = email, Password = password });
        loginResponse.EnsureSuccessStatusCode();

        var body = await loginResponse.Content.ReadFromJsonAsync<AuthenticationResponse>();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", body!.Token);
        return client;
    }

    // Seeds a Hotel (if none exists yet) + Room directly via DbContext -- bypassing the
    // admin-only create-room endpoint, which isn't what this test is about -- and a
    // Reservation owned by whichever customer is authenticated on ownerClient. Returns the
    // reservation's id.
    private async Task<Guid> SeedRoomAndReservationForCurrentOwnerAsync(HttpClient ownerClient)
    {
        var meResponse = await ownerClient.GetAsync("/api/customers/mine");
        meResponse.EnsureSuccessStatusCode();
        var me = await meResponse.Content.ReadFromJsonAsync<CustomerDto>();

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<HotelDbContext>();

        var hotel = await db.Hotels.FirstOrDefaultAsync();
        if (hotel == null)
        {
            hotel = new Hotel("Test Hotel", "1 Test St");
            db.Hotels.Add(hotel);
            await db.SaveChangesAsync();
        }

        var room = new Room(Guid.NewGuid().ToString("N")[..8], RoomType.Single, 100m, hotel.Id);
        db.Rooms.Add(room);
        await db.SaveChangesAsync();

        var reservation = new Reservation(room.Id, me!.Id, DateTime.UtcNow.AddDays(5), DateTime.UtcNow.AddDays(7), 100m);
        db.Reservations.Add(reservation);
        await db.SaveChangesAsync();

        return reservation.Id;
    }
}

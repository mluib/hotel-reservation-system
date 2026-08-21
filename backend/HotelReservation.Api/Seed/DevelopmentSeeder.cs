using HotelReservation.Application.Interfaces;
using HotelReservation.Domain.Entities;
using HotelReservation.Domain.Enums;
using HotelReservation.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace HotelReservation.Api.Seed;

/// <summary>
/// Development-only startup seeding (see call sites in <c>Program.cs</c>, both gated
/// behind <c>IsDevelopment()</c>). Split into two independent methods -- roles/admin vs.
/// demo data -- so either can be skipped or reasoned about on its own; pulled out of
/// <c>Program.cs</c> so that file stays focused on host/service wiring.
/// </summary>
public static class DevelopmentSeeder
{
    /// <summary>
    /// Ensures the Admin/Customer Identity roles exist and that one admin login is
    /// available to sign in with, sourced from configuration rather than hardcoded.
    /// Provisional -- proper role/user seeding is deferred to the Phase 6 backlog
    /// ("JWT: role/user seeding") for production-appropriate config/secrets handling.
    /// </summary>
    public static async Task SeedRolesAndAdminAsync(WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var services = scope.ServiceProvider;

        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
        foreach (var role in new[] { "Admin", "Customer" })
        {
            if (!await roleManager.RoleExistsAsync(role))
                await roleManager.CreateAsync(new IdentityRole(role));
        }

        var adminEmail = app.Configuration["Seed:AdminEmail"];
        var adminPassword = app.Configuration["Seed:AdminPassword"];
        if (string.IsNullOrWhiteSpace(adminEmail) || string.IsNullOrWhiteSpace(adminPassword))
            return;

        var userManager = services.GetRequiredService<UserManager<IdentityUser>>();
        var existing = await userManager.FindByEmailAsync(adminEmail);
        if (existing != null)
            return;

        var adminUser = new IdentityUser { UserName = adminEmail, Email = adminEmail, EmailConfirmed = true };
        var result = await userManager.CreateAsync(adminUser, adminPassword);
        if (result.Succeeded)
            await userManager.AddToRoleAsync(adminUser, "Admin");
    }

    /// <summary>
    /// Seeds one demo hotel and three rooms -- one of each <see cref="RoomType"/> --
    /// complete with placeholder photos, so a freshly started container (e.g.
    /// <c>docker compose up</c>) shows a populated frontend instead of an empty one.
    /// Guarded by "any Hotel row already exists", not by whether the rooms/photos
    /// specifically look seeded, so it runs at most once per database and never
    /// overwrites data an admin has since edited through the UI. Customers/reservations
    /// are deliberately left unseeded -- registering/logging in as a customer is itself
    /// part of the demo, and an admin login already comes from
    /// <see cref="SeedRolesAndAdminAsync"/>.
    /// </summary>
    /// <remarks>
    /// Also guarded by <c>IsSqlServer()</c>, same as the migration step above in
    /// <c>Program.cs</c> and for the same reason: HotelReservation.Tests.Integration's
    /// <c>CustomWebApplicationFactory</c> runs as "Development" too but swaps in SQLite,
    /// and every test run spins up a fresh one. Without this guard, each test run would
    /// seed a throwaway hotel/rooms into that SQLite database <i>and</i> copy real image
    /// files into wwwroot/uploads on disk -- with a new random file name each time, since
    /// these entities get a fresh Id per run.
    /// </remarks>
    public static async Task SeedDemoDataAsync(WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var services = scope.ServiceProvider;

        var db = services.GetRequiredService<HotelDbContext>();
        if (!db.Database.IsSqlServer())
            return;
        if (await db.Hotels.AnyAsync())
            return;

        var imageStorage = services.GetRequiredService<IImageStorageService>();
        var seedImagesPath = Path.Combine(app.Environment.WebRootPath, "seed-images");

        var hotel = new Hotel("Bussen Lodge", "12 Summit Road, Hillcrest");
        db.Hotels.Add(hotel);
        await db.SaveChangesAsync();

        hotel.SetImage(await SaveSeedImageAsync(imageStorage, seedImagesPath, "hotel.jpg", "hotel", hotel.Id));

        var rooms = new[]
        {
            (Room: new Room("101", RoomType.Single, 89m, hotel.Id), Image: "room-single.jpg"),
            (Room: new Room("102", RoomType.Double, 129m, hotel.Id), Image: "room-double.jpg"),
            (Room: new Room("103", RoomType.Suite, 219m, hotel.Id), Image: "room-suite.jpg"),
        };
        db.Rooms.AddRange(rooms.Select(r => r.Room));
        await db.SaveChangesAsync();

        foreach (var (room, image) in rooms)
        {
            room.SetImage(await SaveSeedImageAsync(imageStorage, seedImagesPath, image, "rooms", room.Id));
        }

        await db.SaveChangesAsync();
    }

    /// <summary>
    /// Copies a placeholder image from wwwroot/seed-images (checked into git) into the
    /// same uploads/&lt;subfolder&gt;/&lt;id&gt;.jpg location a real admin upload would
    /// use, via the same <see cref="IImageStorageService"/> the upload endpoints use --
    /// so seeded photos are served and replaced exactly like any other uploaded image.
    /// </summary>
    private static async Task<string> SaveSeedImageAsync(
        IImageStorageService imageStorage, string seedImagesPath, string sourceFileName, string subfolder, Guid entityId)
    {
        await using var source = File.OpenRead(Path.Combine(seedImagesPath, sourceFileName));
        var extension = Path.GetExtension(sourceFileName);
        return await imageStorage.SaveAsync(source, $"{entityId}{extension}", subfolder);
    }
}

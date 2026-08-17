using System;
using System.Collections.Generic;
using System.Text;

using HotelReservation.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;

namespace HotelReservation.Infrastructure.Persistence;

public class HotelDbContext : IdentityDbContext<IdentityUser>
{
    public HotelDbContext(
        DbContextOptions<HotelDbContext> options)
        : base(options)
    {
    }

    public DbSet<Hotel> Hotels { get; set; }

    public DbSet<Room> Rooms { get; set; }

    public DbSet<Customer> Customers { get; set; }

    public DbSet<Reservation> Reservations { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // First real Fluent API configuration in the project -- needed for the value
        // objects (EmailAddress/Money/DateRange) introduced in Phase 6, which convention-based
        // mapping can't handle on its own.
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(HotelDbContext).Assembly);
    }
}
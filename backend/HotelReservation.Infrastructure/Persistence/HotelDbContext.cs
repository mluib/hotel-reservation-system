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
}
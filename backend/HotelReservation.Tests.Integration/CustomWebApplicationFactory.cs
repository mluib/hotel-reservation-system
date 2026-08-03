using System;
using System.Linq;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using HotelReservation.Infrastructure.Persistence;

namespace HotelReservation.Tests.Integration;

/// <summary>
/// Custom WebApplicationFactory that replaces the production DB with an in-memory SQLite
/// using a single open SqliteConnection for the lifetime of the factory.
/// Ensures the database schema is created for each test run and avoids using the developer SQL Server.
/// </summary>
public class CustomWebApplicationFactory : WebApplicationFactory<HotelReservation.Api.Program>
{
    private SqliteConnection? _connection;

    protected override IHost CreateHost(IHostBuilder builder)
    {
        // Ensure we run in the test environment
        builder.UseEnvironment("Development");
        return base.CreateHost(builder);
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // Remove any existing DbContext and EF Core provider registrations so we can replace them with SQLite.
            // Some registrations may come from Identity/AddEntityFrameworkStores or AddDbContext; remove broadly.
            var toRemove = services.Where(d =>
                (d.ServiceType != null && d.ServiceType.FullName != null && d.ServiceType.FullName.StartsWith("Microsoft.EntityFrameworkCore")) ||
                (d.ImplementationType != null && d.ImplementationType.FullName != null && d.ImplementationType.FullName.StartsWith("Microsoft.EntityFrameworkCore")) ||
                (d.ServiceType == typeof(DbContextOptions<HotelDbContext>)) ||
                (d.ServiceType == typeof(HotelDbContext)) ||
                (d.ImplementationType != null && (d.ImplementationType == typeof(HotelDbContext) || d.ImplementationType.IsSubclassOf(typeof(DbContext)))) ||
                (d.ServiceType.FullName?.Contains("SqlServer") == true) ||
                (d.ImplementationType?.FullName?.Contains("SqlServer") == true)
            ).ToList();

            foreach (var d in toRemove)
                services.Remove(d);

            // Open SQLite in-memory connection that persists for the lifetime of the factory
            _connection = new SqliteConnection("DataSource=:memory:");
            _connection.Open();

            // Register HotelDbContext to use SQLite in-memory
            // Build an isolated internal service provider for SQLite to avoid provider conflicts
            var sqliteServices = new ServiceCollection()
                .AddEntityFrameworkSqlite()
                .BuildServiceProvider();

            services.AddDbContext<HotelDbContext>(options =>
            {
                options.UseSqlite(_connection);
                options.UseInternalServiceProvider(sqliteServices);
            });

            // Build the service provider and ensure database is created
            var sp = services.BuildServiceProvider();
            using (var scope = sp.CreateScope())
            {
                var scopedServices = scope.ServiceProvider;
                var db = scopedServices.GetRequiredService<HotelDbContext>();
                // Ensure database created (applies model, not migrations)
                db.Database.EnsureCreated();
            }
        });
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (disposing)
        {
            if (_connection != null)
            {
                try { _connection.Close(); } catch { }
                _connection.Dispose();
                _connection = null;
            }
        }
    }
}

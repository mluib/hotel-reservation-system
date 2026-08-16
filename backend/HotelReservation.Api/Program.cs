
using System.Text.Json.Serialization;
using HotelReservation.Api.Middleware;
using HotelReservation.Application.Customers;
using HotelReservation.Application.Hotels;
using HotelReservation.Application.Interfaces;
using Microsoft.OpenApi.Models;
using HotelReservation.Application.Reservations;
using HotelReservation.Application.Rooms;
using HotelReservation.Infrastructure.Persistence;
using HotelReservation.Infrastructure.Repositories;
using HotelReservation.Infrastructure.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Serilog;

namespace HotelReservation.Api
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            // Bootstrap logger: covers anything that goes wrong before the full pipeline
            // (configured from appsettings' "Serilog" section, wired up via UseSerilog
            // below) exists yet -- e.g. bad configuration or a host build failure.
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Information()
                .WriteTo.Console()
                .CreateBootstrapLogger();

            try
            {
                await RunAsync(args);
            }
            // WebApplicationFactory (used by HotelReservation.Tests.Integration) builds the
            // host by invoking this Main method and relies on a HostAbortedException
            // unwinding back out of it once the builder is captured, without ever calling
            // Run(). A catch-all here would otherwise swallow that and break the test host.
            catch (Exception ex) when (ex is not HostAbortedException)
            {
                Log.Fatal(ex, "Application terminated unexpectedly");
            }
            finally
            {
                Log.CloseAndFlush();
            }
        }

        private static async Task RunAsync(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Replace the bootstrap logger with the fully configured one now that
            // builder.Configuration (appsettings + env) is available. Built eagerly from
            // Log.Logger rather than via UseSerilog's lazy (context, services, config)
            // overload: that overload wraps a ReloadableLogger which freezes on first use
            // and throws if the host is ever built a second time -- which is exactly what
            // WebApplicationFactory (HotelReservation.Tests.Integration) does internally.
            Log.Logger = new LoggerConfiguration()
                .ReadFrom.Configuration(builder.Configuration)
                .Enrich.FromLogContext()
                .CreateLogger();
            builder.Host.UseSerilog();

            // Add services to the container.
            {
                builder.Services.AddControllers()
                    // Serialize enums (RoomType, ReservationStatus) as their names ("Single",
                    // "Confirmed") instead of the default underlying int, so API consumers
                    // don't have to hardcode numeric-to-name mappings.
                    .AddJsonOptions(options =>
                        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

                builder.Services.AddEndpointsApiExplorer();

                // CORS: allow the Angular dev server to call this API. Origins are read from
                // Cors:AllowedOrigins (set in appsettings.Development.json to localhost:4200)
                // so a Docker/staging origin can be added later without touching this code;
                // the literal below is only a fallback if that config section is ever missing.
                var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
                    ?? new[] { "http://localhost:4200" };
                builder.Services.AddCors(options =>
                {
                    options.AddPolicy("Frontend", policy =>
                        policy.WithOrigins(allowedOrigins)
                            .AllowAnyHeader()
                            .AllowAnyMethod());
                });

                // OpenAPI
                // Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
                // URL: https://localhost:7290/openapi/v1.json
                // Attention: needs Microsoft.AspNetCore.OpenApi, which conflicts with Swashbuckle 6.5.0
                //builder.Services.AddOpenApi();

                // Swagger
                // URL: https://localhost:7290/swagger
                // Attention: newest version 10.2.3 of Swashbuckle can't be configured correctly, so use 6.5.0 instead
                builder.Services.AddSwaggerGen(c =>
                {
                    // Enable JWT authentication in Swagger
                    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                    {
                        Description = "JWT Authorization header using the Bearer scheme. Example: 'Authorization: Bearer {token}'",
                        Name = "Authorization",
                        In = ParameterLocation.Header,
                        Type = SecuritySchemeType.Http,
                        Scheme = "bearer",
                        BearerFormat = "JWT"
                    });

                    c.AddSecurityRequirement(new OpenApiSecurityRequirement
                    {
                        {
                            new OpenApiSecurityScheme
                            {
                                Reference = new OpenApiReference
                                {
                                    Type = ReferenceType.SecurityScheme,
                                    Id = "Bearer"
                                }
                            },
                            Array.Empty<string>()
                        }
                    });
                });

                // Database
                builder.Services.AddDbContext<HotelDbContext>(options =>
                    options.UseSqlServer(
                        builder.Configuration.GetConnectionString("DefaultConnection")));

                // Authentication & Authorization
                {
                    // Identity
                    builder.Services.AddIdentity<Microsoft.AspNetCore.Identity.IdentityUser, Microsoft.AspNetCore.Identity.IdentityRole>()
                    .AddEntityFrameworkStores<HotelDbContext>();

                    // JWT Authentication
                    var jwtKey = builder.Configuration["Jwt:Key"] ?? "Q2hhbmdlVGhpc0RldktleTEyMzQ1Njc4OTAxMjM0NTY3ODkw";
                    var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "hotel";
                    var jwtAudience = builder.Configuration["Jwt:Audience"] ?? "hotel_audience";

                    builder.Services.AddAuthentication(options =>
                    {
                        options.DefaultAuthenticateScheme = Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerDefaults.AuthenticationScheme;
                        options.DefaultChallengeScheme = Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerDefaults.AuthenticationScheme;
                    })
                    .AddJwtBearer(options =>
                    {
                        options.TokenValidationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters
                        {
                            ValidateIssuer = true,
                            ValidIssuer = jwtIssuer,
                            ValidateAudience = true,
                            ValidAudience = jwtAudience,
                            ValidateLifetime = true,
                            ValidateIssuerSigningKey = true,
                            IssuerSigningKey = new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(Convert.FromBase64String(jwtKey))
                        };
                    });

                    builder.Services.AddAuthorization();

                    builder.Services.AddScoped<Application.Interfaces.IJwtTokenService, Infrastructure.Services.JwtTokenService>();
                    builder.Services.AddScoped<Application.Interfaces.IAuthService, Infrastructure.Services.AuthService>();
                }

                // Accessor for current user (used by application services to enforce ownership)
                builder.Services.AddHttpContextAccessor();
                builder.Services.AddScoped<ICurrentUserService, Services.CurrentUserService>();

                // Reservation Use-Case
                builder.Services.AddScoped<IReservationRepository, ReservationRepository>();
                builder.Services.AddScoped<CreateReservation>();
                builder.Services.AddScoped<GetReservations>();
                builder.Services.AddScoped<GetReservationById>();
                builder.Services.AddScoped<GetMyReservations>();
                builder.Services.AddScoped<DeleteReservation>();
                builder.Services.AddScoped<CancelReservation>();

                // Customer Use-Case
                builder.Services.AddScoped<ICustomerRepository, CustomerRepository>();
                builder.Services.AddScoped<GetCustomerById>();
                builder.Services.AddScoped<GetCustomers>();
                builder.Services.AddScoped<UpdateCustomer>();
                builder.Services.AddScoped<DeleteCustomer>();
                builder.Services.AddScoped<GetCurrentCustomer>();

                // Room Use-Case
                builder.Services.AddScoped<IRoomRepository, RoomRepository>();
                builder.Services.AddScoped<CreateRoom>();
                builder.Services.AddScoped<GetRooms>();
                builder.Services.AddScoped<GetRoomById>();
                builder.Services.AddScoped<UpdateRoom>();
                builder.Services.AddScoped<DeleteRoom>();
                builder.Services.AddScoped<UploadRoomImage>();

                // Hotel Use-Case
                builder.Services.AddScoped<IHotelRepository, HotelRepository>();
                builder.Services.AddScoped<GetHotel>();
                builder.Services.AddScoped<UpdateHotel>();
                builder.Services.AddScoped<UploadHotelImage>();

                // Image storage (local disk, under wwwroot)
                var webRootPath = builder.Environment.WebRootPath
                    ?? Path.Combine(builder.Environment.ContentRootPath, "wwwroot");
                builder.Services.AddSingleton<IImageStorageService>(
                    _ => new ImageStorageService(webRootPath));
            }

            var app = builder.Build();

            // Apply pending EF Core migrations automatically on startup, so a fresh
            // container (or any real SQL Server target) always has an up-to-date schema
            // without a separate manual migration step. Guarded by IsSqlServer(), not by
            // environment: HotelReservation.Tests.Integration's CustomWebApplicationFactory
            // also runs as "Development" but swaps in a SQLite provider for tests, so an
            // environment check alone wouldn't have excluded it — the provider check does.
            // also: migration anyway is only consistent for SQL Server
            using (var scope = app.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<HotelDbContext>();
                if (db.Database.IsSqlServer())
                {
                    await db.Database.MigrateAsync();
                }
            }

            // Configure the HTTP request pipeline.
            {
                // One structured line per request (method, path, status code, elapsed ms),
                // replacing the framework's own multi-line-per-request logging. Registered
                // first so it wraps every other middleware, including the exception handler
                // below, and reports the true final status code and duration.
                app.UseSerilogRequestLogging();

                // Catches anything no controller/use case already handles, logs it, and
                // returns a generic error instead of an unhandled 500 with no trace anywhere.
                app.UseMiddleware<ExceptionHandlingMiddleware>();

                if (app.Environment.IsDevelopment())
                {
                    // OpenAPI
                    //app.MapOpenApi();

                    // Swagger
                    app.UseSwagger();
                    app.UseSwaggerUI();
                }

                app.UseHttpsRedirection();
                app.UseStaticFiles();

                app.UseCors("Frontend");

                app.UseAuthentication();
                app.UseAuthorization();

                app.MapControllers();
            }

            // Dev-only seed: without this there's no way to obtain an Admin account, since
            // registration always assigns "Customer" and there's no promotion mechanism yet.
            // Provisional — proper role/user seeding is deferred to the Phase 6 backlog
            // ("JWT: role/user seeding") for production-appropriate config/secrets handling.
            if (app.Environment.IsDevelopment())
            {
                await SeedDevAdminAsync(app);
            }

            await app.RunAsync();
        }

        // Ensures the Admin/Customer Identity roles exist and that one admin login is
        // available to sign in with, sourced from configuration rather than hardcoded.
        // Development-only (see call site) — not a production seeding strategy.
        private static async Task SeedDevAdminAsync(WebApplication app)
        {
            using var scope = app.Services.CreateScope();
            var services = scope.ServiceProvider;

            var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
            foreach (var role in new[] { "Admin", "Customer" })
            {
                if (!await roleManager.RoleExistsAsync(role))
                    await roleManager.CreateAsync(new IdentityRole(role));
            }

            // The system is designed around exactly one Hotel row always existing (see
            // UpdateHotel/GetHotel, which look it up with no id — there is no "create hotel"
            // endpoint at all). A brand-new database therefore has no way to ever get that
            // first row through the UI. Seed a placeholder here, independent of the admin-user
            // check below, so it runs on every startup regardless of whether the admin already
            // exists. Dev-only, same as the rest of this method.
            var hotelDbContext = services.GetRequiredService<HotelReservation.Infrastructure.Persistence.HotelDbContext>();
            if (!await hotelDbContext.Hotels.AnyAsync())
            {
                hotelDbContext.Hotels.Add(new HotelReservation.Domain.Entities.Hotel("Hotel One", "123 Main St"));
                await hotelDbContext.SaveChangesAsync();
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
    }
}

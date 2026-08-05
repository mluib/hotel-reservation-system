
using System.Text.Json.Serialization;
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

namespace HotelReservation.Api
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

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

                    builder.Services.AddScoped<Application.Authentication.IJwtTokenService, Infrastructure.Services.JwtTokenService>();
                    builder.Services.AddScoped<Application.Authentication.IAuthService, Infrastructure.Services.AuthService>();
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

            // Configure the HTTP request pipeline.
            {
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

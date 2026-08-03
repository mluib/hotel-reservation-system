
using HotelReservation.Application.Customers;
using HotelReservation.Application.Hotels;
using HotelReservation.Application.Interfaces;
using Microsoft.OpenApi.Models;
using HotelReservation.Application.Reservations;
using HotelReservation.Application.Rooms;
using HotelReservation.Infrastructure.Persistence;
using HotelReservation.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;

namespace HotelReservation.Api
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            {
                builder.Services.AddControllers();

                builder.Services.AddEndpointsApiExplorer();

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

                // Customer Use-Case
                builder.Services.AddScoped<ICustomerRepository, CustomerRepository>();
                builder.Services.AddScoped<GetCustomerById>();
                builder.Services.AddScoped<GetCustomers>();
                builder.Services.AddScoped<UpdateCustomer>();
                builder.Services.AddScoped<DeleteCustomer>();

                // Room Use-Case
                builder.Services.AddScoped<IRoomRepository, RoomRepository>();
                builder.Services.AddScoped<CreateRoom>();
                builder.Services.AddScoped<GetRooms>();
                builder.Services.AddScoped<GetRoomById>();
                builder.Services.AddScoped<UpdateRoom>();
                builder.Services.AddScoped<DeleteRoom>();

                // Hotel Use-Case
                builder.Services.AddScoped<IHotelRepository, HotelRepository>();
                builder.Services.AddScoped<GetHotel>();
                builder.Services.AddScoped<UpdateHotel>();
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

                app.UseAuthentication();
                app.UseAuthorization();

                app.MapControllers();
            }

            app.Run();
        }
    }
}


using HotelReservation.Application.Interfaces;
using HotelReservation.Application.Reservations;
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

                // OpenAPI
                // Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
                // URL: https://localhost:7290/openapi/v1.json
                builder.Services.AddOpenApi();

                // Swagger
                // URL: https://localhost:7290/swagger
                builder.Services.AddSwaggerGen();

                // Database
                builder.Services.AddDbContext<HotelDbContext>(options =>
                    options.UseSqlServer(
                        builder.Configuration.GetConnectionString("DefaultConnection")));

                builder.Services.AddScoped<IReservationRepository, ReservationRepository>();

                builder.Services.AddScoped<CreateReservation>();
            }

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            {
                if (app.Environment.IsDevelopment())
                {
                    // OpenAPI
                    app.MapOpenApi();

                    // Swagger
                    app.UseSwagger();
                    app.UseSwaggerUI();
                }

                app.UseHttpsRedirection();

                app.UseAuthorization();

                app.MapControllers();
            }

            app.Run();
        }
    }
}

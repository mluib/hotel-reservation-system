
using HotelReservation.Application.Interfaces;
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

                // Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
                builder.Services.AddOpenApi();

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
                    app.MapOpenApi();
                }

                app.UseHttpsRedirection();

                app.UseAuthorization();

                app.MapControllers();
            }

            app.Run();
        }
    }
}


using HotelReservation.Application.Interfaces;
using HotelReservation.Application.Reservations;
using HotelReservation.Application.Customers;
using HotelReservation.Application.Rooms;
using HotelReservation.Application.Hotels;
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
                builder.Services.AddScoped<ICustomerRepository, CustomerRepository>();
                builder.Services.AddScoped<IRoomRepository, RoomRepository>();
                builder.Services.AddScoped<IHotelRepository, HotelRepository>();

                builder.Services.AddScoped<CreateReservation>();
                builder.Services.AddScoped<GetReservations>();
                builder.Services.AddScoped<GetReservationById>();
                builder.Services.AddScoped<DeleteReservation>();

                builder.Services.AddScoped<CreateCustomer>();
                builder.Services.AddScoped<GetCustomerById>();
                builder.Services.AddScoped<GetCustomers>();
                builder.Services.AddScoped<UpdateCustomer>();
                builder.Services.AddScoped<DeleteCustomer>();

                builder.Services.AddScoped<CreateRoom>();
                builder.Services.AddScoped<GetRooms>();
                builder.Services.AddScoped<GetRoomById>();
                builder.Services.AddScoped<UpdateRoom>();
                builder.Services.AddScoped<DeleteRoom>();

                builder.Services.AddScoped<GetHotel>();
                builder.Services.AddScoped<UpdateHotel>();
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

using HotelReservation.Domain.Entities;

namespace HotelReservation.Application.Interfaces;

public interface IHotelRepository
{
    Task<Hotel?> GetAsync();

    Task UpdateAsync(Hotel hotel);
}

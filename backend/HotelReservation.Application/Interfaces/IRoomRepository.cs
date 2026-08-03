using HotelReservation.Domain.Entities;

namespace HotelReservation.Application.Interfaces;

public interface IRoomRepository
{
    Task AddAsync(Room room);

    Task<Room?> GetByIdAsync(Guid id);

    Task<IEnumerable<Room>> GetAllAsync();

    Task UpdateAsync(Room room);

    Task DeleteAsync(Room room);
}

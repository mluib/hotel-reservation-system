using HotelReservation.Application.Interfaces;

namespace HotelReservation.Application.Rooms;

public class DeleteRoom
{
    private readonly IRoomRepository _repository;

    public DeleteRoom(IRoomRepository repository)
    {
        _repository = repository;
    }

    public async Task ExecuteAsync(System.Guid id)
    {
        var existing = await _repository.GetByIdAsync(id);
        if (existing == null) throw new InvalidOperationException("Room not found.");

        await _repository.DeleteAsync(existing);
    }
}

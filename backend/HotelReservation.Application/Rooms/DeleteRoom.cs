using HotelReservation.Application.Common.Exceptions;
using HotelReservation.Application.Interfaces;

namespace HotelReservation.Application.Rooms;

public class DeleteRoom
{
    private readonly IRoomRepository _repository;
    private readonly IReservationRepository _reservationRepository;

    public DeleteRoom(IRoomRepository repository, IReservationRepository reservationRepository)
    {
        _repository = repository;
        _reservationRepository = reservationRepository;
    }

    public async Task ExecuteAsync(System.Guid id)
    {
        var existing = await _repository.GetByIdAsync(id);
        if (existing == null) throw new NotFoundException("Room not found.");

        var hasReservations = await _reservationRepository.ExistsForRoomAsync(id);
        if (hasReservations) throw new ConflictException("Cannot delete a room that has reservations.");

        await _repository.DeleteAsync(existing);
    }
}

using HotelReservation.Application.Common.Exceptions;
using HotelReservation.Application.DTOs;
using HotelReservation.Application.Interfaces;
using HotelReservation.Domain.Entities;

namespace HotelReservation.Application.Rooms;

public class UpdateRoom
{
    private readonly IRoomRepository _repository;

    public UpdateRoom(IRoomRepository repository)
    {
        _repository = repository;
    }

    public async Task ExecuteAsync(System.Guid id, RoomRequest request)
    {
        var existing = await _repository.GetByIdAsync(id);
        if (existing == null) throw new NotFoundException("Room not found.");

        // Use domain method to update allowed fields and preserve invariants
        existing.Update(request.Number, request.Type, request.PricePerNight, request.HotelId);

        await _repository.UpdateAsync(existing);
    }
}

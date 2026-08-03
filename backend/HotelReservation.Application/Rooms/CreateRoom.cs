using HotelReservation.Application.DTOs;
using HotelReservation.Application.Interfaces;
using HotelReservation.Domain.Entities;

namespace HotelReservation.Application.Rooms;

public class CreateRoom
{
    private readonly IRoomRepository _repository;

    public CreateRoom(IRoomRepository repository)
    {
        _repository = repository;
    }

    public async Task<Guid> ExecuteAsync(CreateRoomRequest request)
    {
        var room = new Room(request.Number, request.Type, request.PricePerNight, request.HotelId);
        await _repository.AddAsync(room);
        return room.Id;
    }
}

using HotelReservation.Application.DTOs;
using HotelReservation.Application.Interfaces;

namespace HotelReservation.Application.Rooms;

public class GetRoomById
{
    private readonly IRoomRepository _repository;

    public GetRoomById(IRoomRepository repository)
    {
        _repository = repository;
    }

    public async Task<RoomDto?> ExecuteAsync(System.Guid id)
    {
        var room = await _repository.GetByIdAsync(id);
        if (room == null) return null;

        return new RoomDto
        {
            Id = room.Id,
            Number = room.Number,
            Type = room.Type,
            PricePerNight = room.PricePerNight,
            HotelId = room.HotelId,
            ImageUrl = room.ImageUrl
        };
    }
}

using System.Collections.Generic;
using HotelReservation.Application.DTOs;
using HotelReservation.Application.Interfaces;

namespace HotelReservation.Application.Rooms;

public class GetRooms
{
    private readonly IRoomRepository _repository;

    public GetRooms(IRoomRepository repository)
    {
        _repository = repository;
    }

    public async Task<IEnumerable<RoomDto>> ExecuteAsync(RoomFilterRequest? filter = null)
    {
        var rooms = await _repository.GetAllAsync(filter);

        var list = new List<RoomDto>();
        foreach (var r in rooms)
        {
            list.Add(new RoomDto
            {
                Id = r.Id,
                Number = r.Number,
                Type = r.Type,
                PricePerNight = r.PricePerNight.Amount,
                HotelId = r.HotelId,
                ImageUrl = r.ImageUrl
            });
        }

        return list;
    }
}

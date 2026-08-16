using System.Collections.Generic;
using System.Threading.Tasks;
using HotelReservation.Application.DTOs;
using HotelReservation.Application.Interfaces;

namespace HotelReservation.Application.Reservations;

public class GetReservations
{
    private readonly IReservationRepository _repository;

    public GetReservations(IReservationRepository repository)
    {
        _repository = repository;
    }

    public async Task<IEnumerable<ReservationDto>> ExecuteAsync()
    {
        var reservations = await _repository.GetAllAsync();
        var list = new List<ReservationDto>();
        foreach (var r in reservations)
        {
            list.Add(new ReservationDto
            {
                Id = r.Id,
                RoomId = r.RoomId,
                CustomerId = r.CustomerId,
                CheckIn = r.CheckIn,
                CheckOut = r.CheckOut,
                Status = r.Status,
                PricePerNight = r.PricePerNight
            });
        }

        return list;
    }
}

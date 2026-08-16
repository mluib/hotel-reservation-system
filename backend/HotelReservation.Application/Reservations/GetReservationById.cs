using HotelReservation.Application.DTOs;
using HotelReservation.Application.Interfaces;

namespace HotelReservation.Application.Reservations;

public class GetReservationById
{
    private readonly IReservationRepository _repository;

    public GetReservationById(IReservationRepository repository)
    {
        _repository = repository;
    }

    public async Task<ReservationDto?> ExecuteAsync(System.Guid id)
    {
        var r = await _repository.GetByIdAsync(id);
        if (r == null) return null;

        return new ReservationDto
        {
            Id = r.Id,
            RoomId = r.RoomId,
            CustomerId = r.CustomerId,
            CheckIn = r.CheckIn,
            CheckOut = r.CheckOut,
            Status = r.Status,
            PricePerNight = r.PricePerNight
        };
    }
}

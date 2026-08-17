using HotelReservation.Application.Common.Exceptions;
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

    public async Task<ReservationDto> ExecuteAsync(System.Guid id)
    {
        var r = await _repository.GetByIdAsync(id);
        if (r == null) throw new NotFoundException("Reservation not found.");

        return new ReservationDto
        {
            Id = r.Id,
            RoomId = r.RoomId,
            CustomerId = r.CustomerId,
            CheckIn = r.Stay.CheckIn,
            CheckOut = r.Stay.CheckOut,
            Status = r.Status,
            PricePerNight = r.PricePerNight.Amount
        };
    }
}

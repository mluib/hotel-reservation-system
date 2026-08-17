using HotelReservation.Application.Common.Exceptions;
using HotelReservation.Application.DTOs;
using HotelReservation.Application.Interfaces;
using HotelReservation.Domain.Entities;

namespace HotelReservation.Application.Hotels;

public class GetHotel
{
    private readonly IHotelRepository _repository;

    public GetHotel(IHotelRepository repository)
    {
        _repository = repository;
    }

    public async Task<HotelDto> ExecuteAsync()
    {
        var hotel = await _repository.GetAsync();
        if (hotel == null) throw new NotFoundException("Hotel not found.");

        return new HotelDto
        {
            Id = hotel.Id,
            Name = hotel.Name,
            Address = hotel.Address,
            ImageUrl = hotel.ImageUrl
        };
    }
}

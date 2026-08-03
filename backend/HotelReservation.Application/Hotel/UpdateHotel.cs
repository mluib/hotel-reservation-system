using HotelReservation.Application.DTOs;
using HotelReservation.Application.Interfaces;

namespace HotelReservation.Application.Hotels;

public class UpdateHotel
{
    private readonly IHotelRepository _repository;

    public UpdateHotel(IHotelRepository repository)
    {
        _repository = repository;
    }

    public async Task ExecuteAsync(UpdateHotelRequest request)
    {
        var hotel = await _repository.GetAsync();
        if (hotel == null) throw new InvalidOperationException("Hotel not found.");

        hotel.Update(request.Name, request.Address);

        await _repository.UpdateAsync(hotel);
    }
}

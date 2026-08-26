using HotelReservation.Application.Common;
using HotelReservation.Application.Common.Exceptions;
using HotelReservation.Application.DTOs;
using HotelReservation.Application.Interfaces;

namespace HotelReservation.Application.Hotels;

public class UploadHotelImage
{
    private readonly IHotelRepository _repository;
    private readonly IImageStorageService _imageStorage;

    public UploadHotelImage(IHotelRepository repository, IImageStorageService imageStorage)
    {
        _repository = repository;
        _imageStorage = imageStorage;
    }

    public async Task<HotelDto> ExecuteAsync(ImageUploadRequest request)
    {
        var hotel = await _repository.GetAsync();
        if (hotel == null) throw new NotFoundException("Hotel not found.");

        var content = await ImageValidation.ValidateAsync(request);

        var fileName = $"{hotel.Id}{ImageValidation.GetExtension(request.ContentType)}";
        var url = await _imageStorage.SaveAsync(content, fileName, "hotel");

        hotel.SetImage(url);
        await _repository.UpdateAsync(hotel);

        return new HotelDto
        {
            Id = hotel.Id,
            Name = hotel.Name,
            Address = hotel.Address,
            ImageUrl = hotel.ImageUrl
        };
    }
}

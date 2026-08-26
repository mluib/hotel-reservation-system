using HotelReservation.Application.Common;
using HotelReservation.Application.Common.Exceptions;
using HotelReservation.Application.DTOs;
using HotelReservation.Application.Interfaces;

namespace HotelReservation.Application.Rooms;

public class UploadRoomImage
{
    private readonly IRoomRepository _repository;
    private readonly IImageStorageService _imageStorage;

    public UploadRoomImage(IRoomRepository repository, IImageStorageService imageStorage)
    {
        _repository = repository;
        _imageStorage = imageStorage;
    }

    public async Task<RoomDto> ExecuteAsync(Guid roomId, ImageUploadRequest request)
    {
        var room = await _repository.GetByIdAsync(roomId);
        if (room == null) throw new NotFoundException("Room not found.");

        var content = await ImageValidation.ValidateAsync(request);

        var fileName = $"{room.Id}{ImageValidation.GetExtension(request.ContentType)}";
        var url = await _imageStorage.SaveAsync(content, fileName, "rooms");

        room.SetImage(url);
        await _repository.UpdateAsync(room);

        return new RoomDto
        {
            Id = room.Id,
            Number = room.Number,
            Type = room.Type,
            PricePerNight = room.PricePerNight.Amount,
            HotelId = room.HotelId,
            ImageUrl = room.ImageUrl
        };
    }
}

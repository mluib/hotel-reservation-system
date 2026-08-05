using HotelReservation.Application.Common;
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
        if (room == null) throw new InvalidOperationException("Room not found.");

        ImageValidation.Validate(request);

        var fileName = $"{room.Id}{ImageValidation.GetExtension(request.ContentType)}";
        var url = await _imageStorage.SaveAsync(request.Content, fileName, "rooms");

        room.SetImage(url);
        await _repository.UpdateAsync(room);

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

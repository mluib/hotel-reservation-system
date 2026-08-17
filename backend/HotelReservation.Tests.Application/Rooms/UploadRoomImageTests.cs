using System;
using System.IO;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using Xunit;
using HotelReservation.Application.Rooms;
using HotelReservation.Application.Common.Exceptions;
using HotelReservation.Application.Interfaces;
using HotelReservation.Application.DTOs;
using HotelReservation.Domain.Entities;
using HotelReservation.Domain.Enums;

namespace HotelReservation.Tests.Application.Rooms;

public class UploadRoomImageTests
{
    private static Room MakeRoom()
    {
        return new Room("101", RoomType.Single, 100m, Guid.NewGuid());
    }

    private static ImageUploadRequest MakeRequest(string contentType = "image/png", long length = 1024)
    {
        return new ImageUploadRequest
        {
            Content = new MemoryStream(new byte[length > 0 ? length : 0]),
            ContentType = contentType,
            Length = length
        };
    }

    [Fact]
    public async Task ExecuteAsync_ValidImage_SavesAndUpdatesRoom()
    {
        var room = MakeRoom();

        var roomRepo = new Mock<IRoomRepository>();
        roomRepo.Setup(r => r.GetByIdAsync(room.Id)).ReturnsAsync(room);
        roomRepo.Setup(r => r.UpdateAsync(It.IsAny<Room>())).Returns(Task.CompletedTask).Verifiable();

        var storage = new Mock<IImageStorageService>();
        storage.Setup(s => s.SaveAsync(It.IsAny<Stream>(), $"{room.Id}.png", "rooms"))
            .ReturnsAsync($"/uploads/rooms/{room.Id}.png");

        var useCase = new UploadRoomImage(roomRepo.Object, storage.Object);

        var dto = await useCase.ExecuteAsync(room.Id, MakeRequest());

        dto.ImageUrl.Should().Be($"/uploads/rooms/{room.Id}.png");
        room.ImageUrl.Should().Be($"/uploads/rooms/{room.Id}.png");
        roomRepo.Verify(r => r.UpdateAsync(It.IsAny<Room>()), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_RoomNotFound_Throws()
    {
        var roomRepo = new Mock<IRoomRepository>();
        roomRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((Room?)null);

        var storage = new Mock<IImageStorageService>();

        var useCase = new UploadRoomImage(roomRepo.Object, storage.Object);

        await useCase.Invoking(x => x.ExecuteAsync(Guid.NewGuid(), MakeRequest()))
            .Should().ThrowAsync<NotFoundException>().WithMessage("Room not found.*");

        storage.Verify(s => s.SaveAsync(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task ExecuteAsync_UnsupportedContentType_Throws()
    {
        var room = MakeRoom();

        var roomRepo = new Mock<IRoomRepository>();
        roomRepo.Setup(r => r.GetByIdAsync(room.Id)).ReturnsAsync(room);

        var storage = new Mock<IImageStorageService>();

        var useCase = new UploadRoomImage(roomRepo.Object, storage.Object);

        await useCase.Invoking(x => x.ExecuteAsync(room.Id, MakeRequest(contentType: "application/pdf")))
            .Should().ThrowAsync<ValidationException>();

        storage.Verify(s => s.SaveAsync(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        roomRepo.Verify(r => r.UpdateAsync(It.IsAny<Room>()), Times.Never);
    }

    [Fact]
    public async Task ExecuteAsync_FileTooLarge_Throws()
    {
        var room = MakeRoom();

        var roomRepo = new Mock<IRoomRepository>();
        roomRepo.Setup(r => r.GetByIdAsync(room.Id)).ReturnsAsync(room);

        var storage = new Mock<IImageStorageService>();

        var useCase = new UploadRoomImage(roomRepo.Object, storage.Object);

        var oversized = MakeRequest(length: 6 * 1024 * 1024);

        await useCase.Invoking(x => x.ExecuteAsync(room.Id, oversized))
            .Should().ThrowAsync<ValidationException>();

        storage.Verify(s => s.SaveAsync(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        roomRepo.Verify(r => r.UpdateAsync(It.IsAny<Room>()), Times.Never);
    }
}

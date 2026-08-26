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
        return new Room("Test Room", "101", RoomType.Single, 100m, Guid.NewGuid());
    }

    private static readonly byte[] PngSignature = { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };

    // Defaults to real PNG magic bytes (padded to `length`), not just zeros -- now that
    // ImageValidation checks actual content against the declared type, a fake all-zero
    // buffer would fail every "valid image" test, not only the ones deliberately testing
    // a mismatch.
    private static ImageUploadRequest MakeRequest(string contentType = "image/png", long length = 1024, byte[]? content = null)
    {
        var bytes = content ?? BuildBytesWithSignature(PngSignature, length);
        return new ImageUploadRequest
        {
            Content = new MemoryStream(bytes),
            ContentType = contentType,
            Length = content?.Length ?? length
        };
    }

    private static byte[] BuildBytesWithSignature(byte[] signature, long length)
    {
        var bytes = new byte[length > 0 ? length : 0];
        signature.CopyTo(bytes, 0);
        return bytes;
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
    public async Task ExecuteAsync_ContentTypeDoesNotMatchActualBytes_Throws()
    {
        var room = MakeRoom();

        var roomRepo = new Mock<IRoomRepository>();
        roomRepo.Setup(r => r.GetByIdAsync(room.Id)).ReturnsAsync(room);

        var storage = new Mock<IImageStorageService>();

        var useCase = new UploadRoomImage(roomRepo.Object, storage.Object);

        // Claims to be a PNG via Content-Type, but the actual bytes are plain text --
        // exactly the spoofing ImageValidation.ValidateSignatureAsync exists to catch.
        var spoofed = MakeRequest(contentType: "image/png", content: System.Text.Encoding.UTF8.GetBytes("not actually an image"));

        await useCase.Invoking(x => x.ExecuteAsync(room.Id, spoofed))
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

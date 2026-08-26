using System;
using System.IO;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using Xunit;
using HotelReservation.Application.Hotels;
using HotelReservation.Application.Common.Exceptions;
using HotelReservation.Application.Interfaces;
using HotelReservation.Application.DTOs;
using HotelReservation.Domain.Entities;

namespace HotelReservation.Tests.Application.Hotels;

public class UploadHotelImageTests
{
    private static Domain.Entities.Hotel MakeHotel()
    {
        return new Domain.Entities.Hotel("Grand Hotel", "1 Main St");
    }

    private static readonly byte[] JpegSignature = { 0xFF, 0xD8, 0xFF };

    // Defaults to real JPEG magic bytes (padded to `length`), not just zeros -- now that
    // ImageValidation checks actual content against the declared type, a fake all-zero
    // buffer would fail every "valid image" test, not only the ones deliberately testing
    // a mismatch.
    private static ImageUploadRequest MakeRequest(string contentType = "image/jpeg", long length = 1024, byte[]? content = null)
    {
        var bytes = content ?? BuildBytesWithSignature(JpegSignature, length);
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
    public async Task ExecuteAsync_ValidImage_SavesAndUpdatesHotel()
    {
        var hotel = MakeHotel();

        var hotelRepo = new Mock<IHotelRepository>();
        hotelRepo.Setup(r => r.GetAsync()).ReturnsAsync(hotel);
        hotelRepo.Setup(r => r.UpdateAsync(It.IsAny<Domain.Entities.Hotel>())).Returns(Task.CompletedTask).Verifiable();

        var storage = new Mock<IImageStorageService>();
        storage.Setup(s => s.SaveAsync(It.IsAny<Stream>(), $"{hotel.Id}.jpg", "hotel"))
            .ReturnsAsync($"/uploads/hotel/{hotel.Id}.jpg");

        var useCase = new UploadHotelImage(hotelRepo.Object, storage.Object);

        var dto = await useCase.ExecuteAsync(MakeRequest());

        dto.ImageUrl.Should().Be($"/uploads/hotel/{hotel.Id}.jpg");
        hotel.ImageUrl.Should().Be($"/uploads/hotel/{hotel.Id}.jpg");
        hotelRepo.Verify(r => r.UpdateAsync(It.IsAny<Domain.Entities.Hotel>()), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_HotelNotFound_Throws()
    {
        var hotelRepo = new Mock<IHotelRepository>();
        hotelRepo.Setup(r => r.GetAsync()).ReturnsAsync((Domain.Entities.Hotel?)null);

        var storage = new Mock<IImageStorageService>();

        var useCase = new UploadHotelImage(hotelRepo.Object, storage.Object);

        await useCase.Invoking(x => x.ExecuteAsync(MakeRequest()))
            .Should().ThrowAsync<NotFoundException>().WithMessage("Hotel not found.*");

        storage.Verify(s => s.SaveAsync(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task ExecuteAsync_UnsupportedContentType_Throws()
    {
        var hotel = MakeHotel();

        var hotelRepo = new Mock<IHotelRepository>();
        hotelRepo.Setup(r => r.GetAsync()).ReturnsAsync(hotel);

        var storage = new Mock<IImageStorageService>();

        var useCase = new UploadHotelImage(hotelRepo.Object, storage.Object);

        await useCase.Invoking(x => x.ExecuteAsync(MakeRequest(contentType: "application/pdf")))
            .Should().ThrowAsync<ValidationException>();

        storage.Verify(s => s.SaveAsync(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        hotelRepo.Verify(r => r.UpdateAsync(It.IsAny<Domain.Entities.Hotel>()), Times.Never);
    }

    [Fact]
    public async Task ExecuteAsync_ContentTypeDoesNotMatchActualBytes_Throws()
    {
        var hotel = MakeHotel();

        var hotelRepo = new Mock<IHotelRepository>();
        hotelRepo.Setup(r => r.GetAsync()).ReturnsAsync(hotel);

        var storage = new Mock<IImageStorageService>();

        var useCase = new UploadHotelImage(hotelRepo.Object, storage.Object);

        // Claims to be a JPEG via Content-Type, but the actual bytes are plain text --
        // exactly the spoofing ImageValidation.ValidateSignatureAsync exists to catch.
        var spoofed = MakeRequest(contentType: "image/jpeg", content: System.Text.Encoding.UTF8.GetBytes("not actually an image"));

        await useCase.Invoking(x => x.ExecuteAsync(spoofed))
            .Should().ThrowAsync<ValidationException>();

        storage.Verify(s => s.SaveAsync(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        hotelRepo.Verify(r => r.UpdateAsync(It.IsAny<Domain.Entities.Hotel>()), Times.Never);
    }

    [Fact]
    public async Task ExecuteAsync_FileTooLarge_Throws()
    {
        var hotel = MakeHotel();

        var hotelRepo = new Mock<IHotelRepository>();
        hotelRepo.Setup(r => r.GetAsync()).ReturnsAsync(hotel);

        var storage = new Mock<IImageStorageService>();

        var useCase = new UploadHotelImage(hotelRepo.Object, storage.Object);

        var oversized = MakeRequest(length: 6 * 1024 * 1024);

        await useCase.Invoking(x => x.ExecuteAsync(oversized))
            .Should().ThrowAsync<ValidationException>();

        storage.Verify(s => s.SaveAsync(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        hotelRepo.Verify(r => r.UpdateAsync(It.IsAny<Domain.Entities.Hotel>()), Times.Never);
    }
}

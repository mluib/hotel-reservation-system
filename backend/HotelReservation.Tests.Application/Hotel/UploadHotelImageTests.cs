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

    private static ImageUploadRequest MakeRequest(string contentType = "image/jpeg", long length = 1024)
    {
        return new ImageUploadRequest
        {
            Content = new MemoryStream(new byte[length > 0 ? length : 0]),
            ContentType = contentType,
            Length = length
        };
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

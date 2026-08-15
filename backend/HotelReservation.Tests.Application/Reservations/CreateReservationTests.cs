using System;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;
using HotelReservation.Application.Reservations;
using HotelReservation.Application.Interfaces;
using HotelReservation.Application.DTOs;
using HotelReservation.Domain.Entities;

namespace HotelReservation.Tests.Application.Reservations;

public class CreateReservationTests
{
    private static Room MakeRoom(decimal pricePerNight = 100m)
    {
        return new Room("101", Domain.Enums.RoomType.Single, pricePerNight, Guid.NewGuid());
    }

    [Fact]
    public async Task ExecuteAsync_InvalidDates_Throws()
    {
        var repoMock = new Mock<IReservationRepository>();
        var roomRepo = new Mock<IRoomRepository>();
        var customerRepo = new Mock<ICustomerRepository>();
        var currentUser = new Mock<ICurrentUserService>();
        currentUser.SetupGet(c => c.UserId).Returns(Guid.NewGuid().ToString());

        var useCase = new CreateReservation(repoMock.Object, roomRepo.Object, currentUser.Object, customerRepo.Object, NullLogger<CreateReservation>.Instance);

        var req = new CreateReservationRequest { RoomId = Guid.NewGuid(), CheckIn = DateTime.UtcNow.AddDays(5), CheckOut = DateTime.UtcNow.AddDays(1) };

        await useCase.Invoking(x => x.ExecuteAsync(req)).Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task ExecuteAsync_Overlapping_Throws()
    {
        var repoMock = new Mock<IReservationRepository>();
        repoMock.Setup(r => r.HasOverlappingReservationAsync(It.IsAny<Guid>(), It.IsAny<DateTime>(), It.IsAny<DateTime>())).ReturnsAsync(true);

        var roomRepo = new Mock<IRoomRepository>();
        roomRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync(MakeRoom());

        var customer = new Domain.Entities.Customer("f","l","test@example.com","id1");
        var customerRepo = new Mock<ICustomerRepository>();
        customerRepo.Setup(c => c.GetByIdentityUserIdAsync(It.IsAny<string>())).ReturnsAsync(customer);

        var currentUser = new Mock<ICurrentUserService>();
        currentUser.SetupGet(c => c.UserId).Returns("id1");

        var useCase = new CreateReservation(repoMock.Object, roomRepo.Object, currentUser.Object, customerRepo.Object, NullLogger<CreateReservation>.Instance);

        var req = new CreateReservationRequest { RoomId = Guid.NewGuid(), CheckIn = DateTime.UtcNow, CheckOut = DateTime.UtcNow.AddDays(1) };

        await useCase.Invoking(x => x.ExecuteAsync(req)).Should().ThrowAsync<InvalidOperationException>().WithMessage("Room is already reserved for this period.*");
    }

    [Fact]
    public async Task ExecuteAsync_Success_AddsReservation()
    {
        var repoMock = new Mock<IReservationRepository>();
        repoMock.Setup(r => r.HasOverlappingReservationAsync(It.IsAny<Guid>(), It.IsAny<DateTime>(), It.IsAny<DateTime>())).ReturnsAsync(false);
        repoMock.Setup(r => r.AddAsync(It.IsAny<Reservation>())).Returns(Task.CompletedTask).Verifiable();

        var roomRepo = new Mock<IRoomRepository>();
        roomRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync(MakeRoom(120m));

        var customer = new Domain.Entities.Customer("f","l","test2@example.com","id2");
        var customerRepo = new Mock<ICustomerRepository>();
        customerRepo.Setup(c => c.GetByIdentityUserIdAsync(It.IsAny<string>())).ReturnsAsync(customer);

        var currentUser = new Mock<ICurrentUserService>();
        currentUser.SetupGet(c => c.UserId).Returns("id2");

        var useCase = new CreateReservation(repoMock.Object, roomRepo.Object, currentUser.Object, customerRepo.Object, NullLogger<CreateReservation>.Instance);

        var req = new CreateReservationRequest { RoomId = Guid.NewGuid(), CheckIn = DateTime.UtcNow, CheckOut = DateTime.UtcNow.AddDays(1) };

        var result = await useCase.ExecuteAsync(req);

        result.Should().NotBeEmpty();
        repoMock.Verify(r => r.AddAsync(It.IsAny<Reservation>()), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_RoomNotFound_Throws()
    {
        var repoMock = new Mock<IReservationRepository>();
        var roomRepo = new Mock<IRoomRepository>();
        roomRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((Room?)null);

        var customerRepo = new Mock<ICustomerRepository>();
        var currentUser = new Mock<ICurrentUserService>();
        currentUser.SetupGet(c => c.UserId).Returns(Guid.NewGuid().ToString());

        var useCase = new CreateReservation(repoMock.Object, roomRepo.Object, currentUser.Object, customerRepo.Object, NullLogger<CreateReservation>.Instance);

        var req = new CreateReservationRequest { RoomId = Guid.NewGuid(), CheckIn = DateTime.UtcNow, CheckOut = DateTime.UtcNow.AddDays(1) };

        await useCase.Invoking(x => x.ExecuteAsync(req)).Should().ThrowAsync<InvalidOperationException>().WithMessage("Room does not exist.*");
    }

    [Fact]
    public async Task ExecuteAsync_Success_SnapshotsRoomPrice()
    {
        var repoMock = new Mock<IReservationRepository>();
        repoMock.Setup(r => r.HasOverlappingReservationAsync(It.IsAny<Guid>(), It.IsAny<DateTime>(), It.IsAny<DateTime>())).ReturnsAsync(false);

        Reservation? added = null;
        repoMock.Setup(r => r.AddAsync(It.IsAny<Reservation>()))
            .Callback<Reservation>(r => added = r)
            .Returns(Task.CompletedTask);

        var roomRepo = new Mock<IRoomRepository>();
        roomRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync(MakeRoom(199.99m));

        var customer = new Domain.Entities.Customer("f","l","test3@example.com","id3");
        var customerRepo = new Mock<ICustomerRepository>();
        customerRepo.Setup(c => c.GetByIdentityUserIdAsync(It.IsAny<string>())).ReturnsAsync(customer);

        var currentUser = new Mock<ICurrentUserService>();
        currentUser.SetupGet(c => c.UserId).Returns("id3");

        var useCase = new CreateReservation(repoMock.Object, roomRepo.Object, currentUser.Object, customerRepo.Object, NullLogger<CreateReservation>.Instance);

        var req = new CreateReservationRequest { RoomId = Guid.NewGuid(), CheckIn = DateTime.UtcNow, CheckOut = DateTime.UtcNow.AddDays(1) };

        await useCase.ExecuteAsync(req);

        added.Should().NotBeNull();
        added!.PricePerNight.Should().Be(199.99m);
    }
}

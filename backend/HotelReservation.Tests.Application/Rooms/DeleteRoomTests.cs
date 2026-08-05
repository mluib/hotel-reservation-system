using System;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using Xunit;
using HotelReservation.Application.Rooms;
using HotelReservation.Application.Interfaces;
using HotelReservation.Domain.Entities;
using HotelReservation.Domain.Enums;

namespace HotelReservation.Tests.Application.Rooms;

public class DeleteRoomTests
{
    private static Room MakeRoom()
    {
        return new Room("101", RoomType.Single, 100m, Guid.NewGuid());
    }

    [Fact]
    public async Task ExecuteAsync_NoReservations_DeletesRoom()
    {
        var room = MakeRoom();

        var roomRepo = new Mock<IRoomRepository>();
        roomRepo.Setup(r => r.GetByIdAsync(room.Id)).ReturnsAsync(room);
        roomRepo.Setup(r => r.DeleteAsync(room)).Returns(Task.CompletedTask).Verifiable();

        var reservationRepo = new Mock<IReservationRepository>();
        reservationRepo.Setup(r => r.ExistsForRoomAsync(room.Id)).ReturnsAsync(false);

        var useCase = new DeleteRoom(roomRepo.Object, reservationRepo.Object);

        await useCase.ExecuteAsync(room.Id);

        roomRepo.Verify(r => r.DeleteAsync(room), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_HasReservations_ThrowsAndDoesNotDelete()
    {
        var room = MakeRoom();

        var roomRepo = new Mock<IRoomRepository>();
        roomRepo.Setup(r => r.GetByIdAsync(room.Id)).ReturnsAsync(room);

        var reservationRepo = new Mock<IReservationRepository>();
        reservationRepo.Setup(r => r.ExistsForRoomAsync(room.Id)).ReturnsAsync(true);

        var useCase = new DeleteRoom(roomRepo.Object, reservationRepo.Object);

        await useCase.Invoking(x => x.ExecuteAsync(room.Id))
            .Should().ThrowAsync<InvalidOperationException>().WithMessage("Cannot delete a room that has reservations.*");

        roomRepo.Verify(r => r.DeleteAsync(It.IsAny<Room>()), Times.Never);
    }

    [Fact]
    public async Task ExecuteAsync_RoomNotFound_Throws()
    {
        var roomRepo = new Mock<IRoomRepository>();
        roomRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((Room?)null);

        var reservationRepo = new Mock<IReservationRepository>();

        var useCase = new DeleteRoom(roomRepo.Object, reservationRepo.Object);

        await useCase.Invoking(x => x.ExecuteAsync(Guid.NewGuid()))
            .Should().ThrowAsync<InvalidOperationException>().WithMessage("Room not found.*");

        reservationRepo.Verify(r => r.ExistsForRoomAsync(It.IsAny<Guid>()), Times.Never);
    }
}

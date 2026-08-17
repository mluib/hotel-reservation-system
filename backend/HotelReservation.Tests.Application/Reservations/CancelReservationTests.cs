using System;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;
using HotelReservation.Application.Reservations;
using HotelReservation.Application.Common.Exceptions;
using HotelReservation.Application.Interfaces;
using HotelReservation.Domain.Entities;
using HotelReservation.Domain.Enums;

namespace HotelReservation.Tests.Application.Reservations;

public class CancelReservationTests
{
    [Fact]
    public async Task ExecuteAsync_OwnReservation_CancelsIt()
    {
        var customer = new Customer("f", "l", "test@example.com", "id1");
        var reservation = new Reservation(Guid.NewGuid(), customer.Id, DateTime.UtcNow, DateTime.UtcNow.AddDays(1), 100m);

        var repoMock = new Mock<IReservationRepository>();
        repoMock.Setup(r => r.GetByIdAsync(reservation.Id)).ReturnsAsync(reservation);
        repoMock.Setup(r => r.UpdateAsync(It.IsAny<Reservation>())).Returns(Task.CompletedTask).Verifiable();

        var customerRepo = new Mock<ICustomerRepository>();
        customerRepo.Setup(c => c.GetByIdentityUserIdAsync(It.IsAny<string>())).ReturnsAsync(customer);

        var currentUser = new Mock<ICurrentUserService>();
        currentUser.SetupGet(c => c.UserId).Returns("id1");

        var useCase = new CancelReservation(repoMock.Object, currentUser.Object, customerRepo.Object, NullLogger<CancelReservation>.Instance);

        await useCase.ExecuteAsync(reservation.Id);

        reservation.Status.Should().Be(ReservationStatus.Cancelled);
        repoMock.Verify(r => r.UpdateAsync(It.IsAny<Reservation>()), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_OtherCustomersReservation_Throws()
    {
        var owner = new Customer("f", "l", "owner@example.com", "owner-id");
        var caller = new Customer("f2", "l2", "caller@example.com", "caller-id");
        var reservation = new Reservation(Guid.NewGuid(), owner.Id, DateTime.UtcNow, DateTime.UtcNow.AddDays(1), 100m);

        var repoMock = new Mock<IReservationRepository>();
        repoMock.Setup(r => r.GetByIdAsync(reservation.Id)).ReturnsAsync(reservation);

        var customerRepo = new Mock<ICustomerRepository>();
        customerRepo.Setup(c => c.GetByIdentityUserIdAsync("caller-id")).ReturnsAsync(caller);

        var currentUser = new Mock<ICurrentUserService>();
        currentUser.SetupGet(c => c.UserId).Returns("caller-id");

        var useCase = new CancelReservation(repoMock.Object, currentUser.Object, customerRepo.Object, NullLogger<CancelReservation>.Instance);

        await useCase.Invoking(x => x.ExecuteAsync(reservation.Id)).Should().ThrowAsync<ForbiddenException>();

        reservation.Status.Should().Be(ReservationStatus.Confirmed);
        repoMock.Verify(r => r.UpdateAsync(It.IsAny<Reservation>()), Times.Never);
    }

    [Fact]
    public async Task ExecuteAsync_Admin_CancelsAnyReservation_WithoutOwnershipCheck()
    {
        var owner = new Customer("f", "l", "owner@example.com", "owner-id");
        var reservation = new Reservation(Guid.NewGuid(), owner.Id, DateTime.UtcNow, DateTime.UtcNow.AddDays(1), 100m);

        var repoMock = new Mock<IReservationRepository>();
        repoMock.Setup(r => r.GetByIdAsync(reservation.Id)).ReturnsAsync(reservation);
        repoMock.Setup(r => r.UpdateAsync(It.IsAny<Reservation>())).Returns(Task.CompletedTask).Verifiable();

        var customerRepo = new Mock<ICustomerRepository>();

        var currentUser = new Mock<ICurrentUserService>();
        currentUser.SetupGet(c => c.UserId).Returns("admin-id");
        currentUser.Setup(c => c.IsInRole("Admin")).Returns(true);

        var useCase = new CancelReservation(repoMock.Object, currentUser.Object, customerRepo.Object, NullLogger<CancelReservation>.Instance);

        await useCase.ExecuteAsync(reservation.Id);

        reservation.Status.Should().Be(ReservationStatus.Cancelled);
        repoMock.Verify(r => r.UpdateAsync(It.IsAny<Reservation>()), Times.Once);
        customerRepo.Verify(c => c.GetByIdentityUserIdAsync(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task ExecuteAsync_ReservationNotFound_Throws()
    {
        var customer = new Customer("f", "l", "test2@example.com", "id2");

        var repoMock = new Mock<IReservationRepository>();
        repoMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((Reservation?)null);

        var customerRepo = new Mock<ICustomerRepository>();
        customerRepo.Setup(c => c.GetByIdentityUserIdAsync(It.IsAny<string>())).ReturnsAsync(customer);

        var currentUser = new Mock<ICurrentUserService>();
        currentUser.SetupGet(c => c.UserId).Returns("id2");

        var useCase = new CancelReservation(repoMock.Object, currentUser.Object, customerRepo.Object, NullLogger<CancelReservation>.Instance);

        await useCase.Invoking(x => x.ExecuteAsync(Guid.NewGuid())).Should().ThrowAsync<NotFoundException>().WithMessage("Reservation not found.*");
    }
}

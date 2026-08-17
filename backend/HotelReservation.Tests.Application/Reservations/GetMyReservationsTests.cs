using System;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using Xunit;
using HotelReservation.Application.Reservations;
using HotelReservation.Application.Common.Exceptions;
using HotelReservation.Application.Interfaces;
using HotelReservation.Domain.Entities;

namespace HotelReservation.Tests.Application.Reservations;

public class GetMyReservationsTests
{
    [Fact]
    public async Task ExecuteAsync_ReturnsOnlyCallersOwnReservations()
    {
        var customer = new Customer("Jane", "Doe", "jane@example.com", "identity-1");
        var otherRoomId = Guid.NewGuid();
        var ownReservation = new Reservation(otherRoomId, customer.Id, DateTime.UtcNow, DateTime.UtcNow.AddDays(2), 120m);

        var customerRepo = new Mock<ICustomerRepository>();
        customerRepo.Setup(c => c.GetByIdentityUserIdAsync("identity-1")).ReturnsAsync(customer);

        var reservationRepo = new Mock<IReservationRepository>();
        reservationRepo.Setup(r => r.GetByCustomerIdAsync(customer.Id))
            .ReturnsAsync(new[] { ownReservation });

        var currentUser = new Mock<ICurrentUserService>();
        currentUser.SetupGet(c => c.UserId).Returns("identity-1");

        var useCase = new GetMyReservations(reservationRepo.Object, currentUser.Object, customerRepo.Object);

        var result = (await useCase.ExecuteAsync()).ToList();

        result.Should().ContainSingle();
        result[0].Id.Should().Be(ownReservation.Id);
        result[0].CustomerId.Should().Be(customer.Id);
        // The only way results could be scoped to someone else's reservations is if this
        // repository call were parameterized wrong -- verifying it was called with this
        // customer's own id is the actual ownership-scoping assertion here.
        reservationRepo.Verify(r => r.GetByCustomerIdAsync(customer.Id), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_Unauthenticated_Throws()
    {
        var customerRepo = new Mock<ICustomerRepository>();
        var reservationRepo = new Mock<IReservationRepository>();

        var currentUser = new Mock<ICurrentUserService>();
        currentUser.SetupGet(c => c.UserId).Returns((string?)null);

        var useCase = new GetMyReservations(reservationRepo.Object, currentUser.Object, customerRepo.Object);

        await useCase.Invoking(x => x.ExecuteAsync()).Should().ThrowAsync<UnauthenticatedException>();
        reservationRepo.Verify(r => r.GetByCustomerIdAsync(It.IsAny<Guid>()), Times.Never);
    }

    [Fact]
    public async Task ExecuteAsync_CustomerNotFound_Throws()
    {
        var customerRepo = new Mock<ICustomerRepository>();
        customerRepo.Setup(c => c.GetByIdentityUserIdAsync("identity-2")).ReturnsAsync((Customer?)null);

        var reservationRepo = new Mock<IReservationRepository>();

        var currentUser = new Mock<ICurrentUserService>();
        currentUser.SetupGet(c => c.UserId).Returns("identity-2");

        var useCase = new GetMyReservations(reservationRepo.Object, currentUser.Object, customerRepo.Object);

        await useCase.Invoking(x => x.ExecuteAsync())
            .Should().ThrowAsync<NotFoundException>().WithMessage("Customer does not exist.*");
    }
}

using System;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using Xunit;
using HotelReservation.Application.Customers;
using HotelReservation.Application.Common.Exceptions;
using HotelReservation.Application.Interfaces;
using HotelReservation.Domain.Entities;

namespace HotelReservation.Tests.Application.Customers;

public class DeleteCustomerTests
{
    private static Customer MakeCustomer()
    {
        return new Customer("Jane", "Doe", "jane@example.com", "identity-1");
    }

    [Fact]
    public async Task ExecuteAsync_NoReservations_DeletesCustomerAndLinkedUser()
    {
        var customer = MakeCustomer();

        var customerRepo = new Mock<ICustomerRepository>();
        customerRepo.Setup(c => c.GetByIdAsync(customer.Id)).ReturnsAsync(customer);
        customerRepo.Setup(c => c.DeleteAsync(customer)).Returns(Task.CompletedTask).Verifiable();

        var reservationRepo = new Mock<IReservationRepository>();
        reservationRepo.Setup(r => r.ExistsForCustomerAsync(customer.Id)).ReturnsAsync(false);

        var authService = new Mock<IAuthService>();
        authService.Setup(a => a.DeleteUserAsync(customer.IdentityUserId!)).Returns(Task.CompletedTask).Verifiable();

        var useCase = new DeleteCustomer(customerRepo.Object, reservationRepo.Object, authService.Object);

        await useCase.ExecuteAsync(customer.Id);

        customerRepo.Verify(c => c.DeleteAsync(customer), Times.Once);
        authService.Verify(a => a.DeleteUserAsync(customer.IdentityUserId!), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_HasReservations_ThrowsAndDoesNotDelete()
    {
        var customer = MakeCustomer();

        var customerRepo = new Mock<ICustomerRepository>();
        customerRepo.Setup(c => c.GetByIdAsync(customer.Id)).ReturnsAsync(customer);

        var reservationRepo = new Mock<IReservationRepository>();
        reservationRepo.Setup(r => r.ExistsForCustomerAsync(customer.Id)).ReturnsAsync(true);

        var authService = new Mock<IAuthService>();

        var useCase = new DeleteCustomer(customerRepo.Object, reservationRepo.Object, authService.Object);

        await useCase.Invoking(x => x.ExecuteAsync(customer.Id))
            .Should().ThrowAsync<ConflictException>().WithMessage("Cannot delete a customer that has reservations.*");

        customerRepo.Verify(c => c.DeleteAsync(It.IsAny<Customer>()), Times.Never);
        authService.Verify(a => a.DeleteUserAsync(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task ExecuteAsync_CustomerNotFound_Throws()
    {
        var customerRepo = new Mock<ICustomerRepository>();
        customerRepo.Setup(c => c.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((Customer?)null);

        var reservationRepo = new Mock<IReservationRepository>();
        var authService = new Mock<IAuthService>();

        var useCase = new DeleteCustomer(customerRepo.Object, reservationRepo.Object, authService.Object);

        await useCase.Invoking(x => x.ExecuteAsync(Guid.NewGuid()))
            .Should().ThrowAsync<NotFoundException>().WithMessage("Customer not found.*");

        reservationRepo.Verify(r => r.ExistsForCustomerAsync(It.IsAny<Guid>()), Times.Never);
    }
}

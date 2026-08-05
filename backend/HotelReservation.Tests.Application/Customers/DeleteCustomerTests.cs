using System;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using Xunit;
using HotelReservation.Application.Customers;
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
    public async Task ExecuteAsync_NoReservations_DeletesCustomer()
    {
        var customer = MakeCustomer();

        var customerRepo = new Mock<ICustomerRepository>();
        customerRepo.Setup(c => c.GetByIdAsync(customer.Id)).ReturnsAsync(customer);
        customerRepo.Setup(c => c.DeleteAsync(customer)).Returns(Task.CompletedTask).Verifiable();

        var reservationRepo = new Mock<IReservationRepository>();
        reservationRepo.Setup(r => r.ExistsForCustomerAsync(customer.Id)).ReturnsAsync(false);

        var useCase = new DeleteCustomer(customerRepo.Object, reservationRepo.Object);

        await useCase.ExecuteAsync(customer.Id);

        customerRepo.Verify(c => c.DeleteAsync(customer), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_HasReservations_ThrowsAndDoesNotDelete()
    {
        var customer = MakeCustomer();

        var customerRepo = new Mock<ICustomerRepository>();
        customerRepo.Setup(c => c.GetByIdAsync(customer.Id)).ReturnsAsync(customer);

        var reservationRepo = new Mock<IReservationRepository>();
        reservationRepo.Setup(r => r.ExistsForCustomerAsync(customer.Id)).ReturnsAsync(true);

        var useCase = new DeleteCustomer(customerRepo.Object, reservationRepo.Object);

        await useCase.Invoking(x => x.ExecuteAsync(customer.Id))
            .Should().ThrowAsync<InvalidOperationException>().WithMessage("Cannot delete a customer that has reservations.*");

        customerRepo.Verify(c => c.DeleteAsync(It.IsAny<Customer>()), Times.Never);
    }

    [Fact]
    public async Task ExecuteAsync_CustomerNotFound_Throws()
    {
        var customerRepo = new Mock<ICustomerRepository>();
        customerRepo.Setup(c => c.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((Customer?)null);

        var reservationRepo = new Mock<IReservationRepository>();

        var useCase = new DeleteCustomer(customerRepo.Object, reservationRepo.Object);

        await useCase.Invoking(x => x.ExecuteAsync(Guid.NewGuid()))
            .Should().ThrowAsync<InvalidOperationException>().WithMessage("Customer not found.*");

        reservationRepo.Verify(r => r.ExistsForCustomerAsync(It.IsAny<Guid>()), Times.Never);
    }
}

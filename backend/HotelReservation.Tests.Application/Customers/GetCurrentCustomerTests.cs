using System;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using Xunit;
using HotelReservation.Application.Customers;
using HotelReservation.Application.Interfaces;
using HotelReservation.Domain.Entities;

namespace HotelReservation.Tests.Application.Customers;

public class GetCurrentCustomerTests
{
    [Fact]
    public async Task ExecuteAsync_LoggedInCustomer_ReturnsOwnProfile()
    {
        var customer = new Customer("Jane", "Doe", "jane@example.com", "identity-1");

        var customerRepo = new Mock<ICustomerRepository>();
        customerRepo.Setup(c => c.GetByIdentityUserIdAsync("identity-1")).ReturnsAsync(customer);

        var currentUser = new Mock<ICurrentUserService>();
        currentUser.SetupGet(c => c.UserId).Returns("identity-1");

        var useCase = new GetCurrentCustomer(customerRepo.Object, currentUser.Object);

        var dto = await useCase.ExecuteAsync();

        dto.Should().NotBeNull();
        dto!.Id.Should().Be(customer.Id);
        dto.FirstName.Should().Be("Jane");
        dto.LastName.Should().Be("Doe");
        dto.Email.Should().Be("jane@example.com");
    }

    [Fact]
    public async Task ExecuteAsync_NoLinkedCustomer_ReturnsNull()
    {
        var customerRepo = new Mock<ICustomerRepository>();
        customerRepo.Setup(c => c.GetByIdentityUserIdAsync("identity-2")).ReturnsAsync((Customer?)null);

        var currentUser = new Mock<ICurrentUserService>();
        currentUser.SetupGet(c => c.UserId).Returns("identity-2");

        var useCase = new GetCurrentCustomer(customerRepo.Object, currentUser.Object);

        var dto = await useCase.ExecuteAsync();

        dto.Should().BeNull();
    }

    [Fact]
    public async Task ExecuteAsync_Unauthenticated_Throws()
    {
        var customerRepo = new Mock<ICustomerRepository>();
        var currentUser = new Mock<ICurrentUserService>();
        currentUser.SetupGet(c => c.UserId).Returns((string?)null);

        var useCase = new GetCurrentCustomer(customerRepo.Object, currentUser.Object);

        await useCase.Invoking(x => x.ExecuteAsync())
            .Should().ThrowAsync<InvalidOperationException>().WithMessage("Unauthenticated user has no profile.*");

        customerRepo.Verify(c => c.GetByIdentityUserIdAsync(It.IsAny<string>()), Times.Never);
    }
}

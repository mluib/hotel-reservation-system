using System;
using FluentAssertions;
using HotelReservation.Domain.Entities;
using Xunit;

namespace HotelReservation.Tests.Domain.Customers;

public class CustomerTests
{
    [Fact]
    public void Constructor_EmptyEmail_Throws()
    {
        Action act = () => new Customer("First","Last", string.Empty);
        act.Should().Throw<ArgumentException>().WithMessage("Email is required.*");
    }

    [Fact]
    public void Update_Valid_ChangesProperties()
    {
        var c = new Customer("A","B","a@b.com");
        c.Update("X","Y","x@y.com");
        c.FirstName.Should().Be("X");
        c.LastName.Should().Be("Y");
        c.Email.Value.Should().Be("x@y.com");
    }
}

using System;
using FluentAssertions;
using HotelReservation.Domain.ValueObjects;
using Xunit;

namespace HotelReservation.Tests.Domain.ValueObjects;

public class EmailAddressTests
{
    [Fact]
    public void Constructor_ValidEmail_TrimsAndSetsValue()
    {
        var email = new EmailAddress("  jane@example.com  ");
        email.Value.Should().Be("jane@example.com");
    }

    [Fact]
    public void Constructor_EmptyValue_Throws()
    {
        Action act = () => new EmailAddress(string.Empty);
        act.Should().Throw<ArgumentException>().WithMessage("Email is required.*");
    }

    [Fact]
    public void Constructor_WhitespaceValue_Throws()
    {
        Action act = () => new EmailAddress("   ");
        act.Should().Throw<ArgumentException>().WithMessage("Email is required.*");
    }

    [Fact]
    public void Constructor_MissingAtSign_Throws()
    {
        Action act = () => new EmailAddress("not-an-email");
        act.Should().Throw<ArgumentException>().WithMessage("Email is not a valid email address.*");
    }

    [Fact]
    public void ToString_ReturnsValue()
    {
        var email = new EmailAddress("jane@example.com");
        email.ToString().Should().Be("jane@example.com");
    }
}

using System;
using FluentAssertions;
using HotelReservation.Domain.ValueObjects;
using Xunit;

namespace HotelReservation.Tests.Domain.ValueObjects;

public class MoneyTests
{
    [Fact]
    public void Constructor_PositiveAmount_SetsPropertiesWithDefaultCurrency()
    {
        var money = new Money(199.99m);

        money.Amount.Should().Be(199.99m);
        money.Currency.Should().Be("EUR");
    }

    [Fact]
    public void Constructor_ExplicitCurrency_IsUppercased()
    {
        var money = new Money(50m, "usd");
        money.Currency.Should().Be("USD");
    }

    [Fact]
    public void Constructor_ZeroAmount_Throws()
    {
        Action act = () => new Money(0m);
        act.Should().Throw<ArgumentException>().WithMessage("Amount must be greater than zero.*");
    }

    [Fact]
    public void Constructor_NegativeAmount_Throws()
    {
        Action act = () => new Money(-10m);
        act.Should().Throw<ArgumentException>().WithMessage("Amount must be greater than zero.*");
    }

    [Fact]
    public void Constructor_InvalidCurrencyLength_Throws()
    {
        Action act = () => new Money(10m, "EU");
        act.Should().Throw<ArgumentException>().WithMessage("Currency must be a 3-letter ISO code.*");
    }

    [Fact]
    public void Equality_SameAmountAndCurrency_AreEqual()
    {
        // Money is a record, so value equality should hold without any custom Equals.
        new Money(100m, "EUR").Should().Be(new Money(100m, "EUR"));
    }
}

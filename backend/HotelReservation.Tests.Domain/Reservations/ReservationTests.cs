using System;
using FluentAssertions;
using HotelReservation.Domain.Entities;
using Xunit;

namespace HotelReservation.Tests.Domain.Reservations;

public class ReservationTests
{
    [Fact]
    public void Constructor_InvalidDates_Throws()
    {
        var roomId = Guid.NewGuid();
        var customerId = Guid.NewGuid();
        Action act = () => new Reservation(roomId, customerId, DateTime.UtcNow.AddDays(1), DateTime.UtcNow, 100m);
        act.Should().Throw<ArgumentException>().WithMessage("Check-out must be after check-in.*");
    }

    [Fact]
    public void Cancel_SetsStatusCancelled()
    {
        var roomId = Guid.NewGuid();
        var customerId = Guid.NewGuid();
        var r = new Reservation(roomId, customerId, DateTime.UtcNow, DateTime.UtcNow.AddDays(1), 100m);
        r.Cancel();
        r.Status.Should().Be(HotelReservation.Domain.Enums.ReservationStatus.Cancelled);
    }

    [Fact]
    public void Constructor_SetsPricePerNight()
    {
        var roomId = Guid.NewGuid();
        var customerId = Guid.NewGuid();
        var r = new Reservation(roomId, customerId, DateTime.UtcNow, DateTime.UtcNow.AddDays(1), 150.50m);
        r.PricePerNight.Amount.Should().Be(150.50m);
    }
}

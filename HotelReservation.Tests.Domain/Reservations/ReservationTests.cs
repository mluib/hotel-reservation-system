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
        Action act = () => new Reservation(roomId, customerId, DateTime.UtcNow.AddDays(1), DateTime.UtcNow);
        act.Should().Throw<ArgumentException>().WithMessage("Check-out must be after check-in.*");
    }

    [Fact]
    public void Cancel_SetsStatusCancelled()
    {
        var roomId = Guid.NewGuid();
        var customerId = Guid.NewGuid();
        var r = new Reservation(roomId, customerId, DateTime.UtcNow, DateTime.UtcNow.AddDays(1));
        r.Cancel();
        r.Status.Should().Be(HotelReservation.Domain.Enums.ReservationStatus.Cancelled);
    }
}

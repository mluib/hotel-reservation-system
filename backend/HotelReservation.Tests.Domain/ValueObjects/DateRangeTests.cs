using System;
using FluentAssertions;
using HotelReservation.Domain.ValueObjects;
using Xunit;

namespace HotelReservation.Tests.Domain.ValueObjects;

public class DateRangeTests
{
    [Fact]
    public void Constructor_CheckOutAfterCheckIn_SetsProperties()
    {
        var checkIn = new DateTime(2027, 1, 10);
        var checkOut = new DateTime(2027, 1, 12);

        var range = new DateRange(checkIn, checkOut);

        range.CheckIn.Should().Be(checkIn);
        range.CheckOut.Should().Be(checkOut);
    }

    [Fact]
    public void Constructor_CheckOutBeforeCheckIn_Throws()
    {
        Action act = () => new DateRange(new DateTime(2027, 1, 12), new DateTime(2027, 1, 10));
        act.Should().Throw<ArgumentException>().WithMessage("Check-out must be after check-in.*");
    }

    [Fact]
    public void Constructor_CheckOutEqualsCheckIn_Throws()
    {
        var same = new DateTime(2027, 1, 10);
        Action act = () => new DateRange(same, same);
        act.Should().Throw<ArgumentException>().WithMessage("Check-out must be after check-in.*");
    }

    [Fact]
    public void Overlaps_OverlappingRanges_ReturnsTrue()
    {
        var a = new DateRange(new DateTime(2027, 1, 1), new DateTime(2027, 1, 10));
        var b = new DateRange(new DateTime(2027, 1, 5), new DateTime(2027, 1, 15));

        a.Overlaps(b).Should().BeTrue();
        b.Overlaps(a).Should().BeTrue();
    }

    [Fact]
    public void Overlaps_AdjacentRanges_ReturnsFalse()
    {
        // b starts exactly when a ends -- back-to-back stays, not an overlap.
        var a = new DateRange(new DateTime(2027, 1, 1), new DateTime(2027, 1, 10));
        var b = new DateRange(new DateTime(2027, 1, 10), new DateTime(2027, 1, 15));

        a.Overlaps(b).Should().BeFalse();
        b.Overlaps(a).Should().BeFalse();
    }

    [Fact]
    public void Overlaps_CompletelySeparateRanges_ReturnsFalse()
    {
        var a = new DateRange(new DateTime(2027, 1, 1), new DateTime(2027, 1, 5));
        var b = new DateRange(new DateTime(2027, 2, 1), new DateTime(2027, 2, 5));

        a.Overlaps(b).Should().BeFalse();
    }
}

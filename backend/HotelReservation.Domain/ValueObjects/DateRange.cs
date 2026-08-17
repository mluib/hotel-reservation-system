namespace HotelReservation.Domain.ValueObjects;

/// <summary>
/// A check-in/check-out pair, replacing the two separate <c>DateTime</c> properties
/// <c>Reservation</c> used to carry directly. Immutable, equality by value.
/// </summary>
public sealed record DateRange
{
    public DateTime CheckIn { get; }

    public DateTime CheckOut { get; }

    public DateRange(DateTime checkIn, DateTime checkOut)
    {
        if (checkOut <= checkIn)
            throw new ArgumentException("Check-out must be after check-in.");

        CheckIn = checkIn;
        CheckOut = checkOut;
    }

    /// <summary>
    /// True if this range and <paramref name="other"/> share any point in time.
    /// </summary>
    /// <remarks>
    /// Centralizes the overlap *definition* for unit testing, but the equivalent expression
    /// still has to be duplicated as raw property comparisons in
    /// <c>ReservationRepository.HasOverlappingReservationAsync</c> and
    /// <c>RoomRepository</c>'s availability filter -- EF Core can't translate an arbitrary
    /// instance method call into SQL, so those two call sites can't call this method directly
    /// without pulling every row into memory first.
    /// </remarks>
    public bool Overlaps(DateRange other) => CheckIn < other.CheckOut && other.CheckIn < CheckOut;
}

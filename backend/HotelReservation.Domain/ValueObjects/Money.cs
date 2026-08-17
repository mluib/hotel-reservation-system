namespace HotelReservation.Domain.ValueObjects;

/// <summary>
/// An amount with a currency, replacing bare <c>decimal</c> prices (<c>Room.PricePerNight</c>,
/// <c>Reservation.PricePerNight</c>). Immutable, equality by value.
/// </summary>
public sealed record Money
{
    public decimal Amount { get; }

    public string Currency { get; }

    public Money(decimal amount, string currency = "EUR")
    {
        if (amount <= 0)
            throw new ArgumentException("Amount must be greater than zero.");

        if (string.IsNullOrWhiteSpace(currency) || currency.Length != 3)
            throw new ArgumentException("Currency must be a 3-letter ISO code.");

        Amount = amount;
        Currency = currency.ToUpperInvariant();
    }
}

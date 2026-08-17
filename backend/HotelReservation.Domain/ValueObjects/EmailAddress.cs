namespace HotelReservation.Domain.ValueObjects;

/// <summary>
/// A validated email address. Immutable, equality by value.
/// </summary>
public sealed record EmailAddress
{
    public string Value { get; }

    public EmailAddress(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Email is required.");

        if (!value.Contains('@'))
            throw new ArgumentException("Email is not a valid email address.");

        Value = value.Trim();
    }

    public override string ToString() => Value;
}

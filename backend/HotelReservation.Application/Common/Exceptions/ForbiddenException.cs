namespace HotelReservation.Application.Common.Exceptions;

/// <summary>
/// The caller is authenticated but not allowed to act on this specific resource
/// (e.g. cancelling another customer's reservation). Maps to 403.
/// </summary>
public sealed class ForbiddenException : AppException
{
    public ForbiddenException(string message) : base(message) { }
}

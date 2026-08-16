namespace HotelReservation.Application.Common.Exceptions;

/// <summary>
/// The request itself is invalid in a way DataAnnotations couldn't catch (e.g. image
/// content-type/size checks that depend on the uploaded file, not just the request shape).
/// Maps to 400.
/// </summary>
public sealed class ValidationException : AppException
{
    public ValidationException(string message) : base(message) { }
}

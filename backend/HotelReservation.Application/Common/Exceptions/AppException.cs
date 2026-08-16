namespace HotelReservation.Application.Common.Exceptions;

/// <summary>
/// Base for the use-case-outcome exceptions below (not-found, conflict, forbidden,
/// validation, unauthenticated).
/// </summary>
/// <remarks>
/// These represent Application-layer decisions about how a request should be rejected --
/// the Api layer's <c>ExceptionHandlingMiddleware</c> maps each concrete type to the
/// matching HTTP status code. Domain entities keep throwing plain <see cref="ArgumentException"/>
/// for their own invariants instead of these, since invariant violations are a Domain concern
/// with no knowledge of HTTP -- the middleware maps <see cref="ArgumentException"/> too, so
/// callers still get the right status code either way.
/// </remarks>
public abstract class AppException : Exception
{
    protected AppException(string message) : base(message) { }
}

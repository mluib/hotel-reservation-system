using HotelReservation.Application.Common.Exceptions;
using Microsoft.AspNetCore.Mvc;

namespace HotelReservation.Api.Middleware;

/// <summary>
/// Central place that turns an unhandled exception into an HTTP response, so individual
/// controllers don't each need their own try/catch.
/// </summary>
/// <remarks>
/// Maps the Application layer's exception taxonomy (<see cref="NotFoundException"/>,
/// <see cref="ConflictException"/>, <see cref="ForbiddenException"/>,
/// <see cref="ValidationException"/>, <see cref="UnauthenticatedException"/>) to the matching
/// status code and a real ProblemDetails (RFC 7807) body. Domain entities' plain
/// <see cref="ArgumentException"/> (invariant violations) map to 400 too, so Domain stays
/// decoupled from HTTP concerns without losing the correct status code. Anything else falls
/// through to a generic 500 with no exception detail in the response.
/// </remarks>
public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            var (status, title) = ex switch
            {
                NotFoundException => (StatusCodes.Status404NotFound, "Not Found"),
                ConflictException => (StatusCodes.Status409Conflict, "Conflict"),
                ForbiddenException => (StatusCodes.Status403Forbidden, "Forbidden"),
                ValidationException => (StatusCodes.Status400BadRequest, "Validation Error"),
                UnauthenticatedException => (StatusCodes.Status401Unauthorized, "Unauthorized"),
                ArgumentException => (StatusCodes.Status400BadRequest, "Validation Error"), // domain invariants
                _ => (StatusCodes.Status500InternalServerError, "An unexpected error occurred.")
            };

            // The taxonomy above (plus ArgumentException) represents expected, handled
            // rejections -- a double-booking or a wrong-role request isn't a bug, so it's
            // logged at Warning, not Error. Anything landing in the 500 branch is genuinely
            // unexpected and stays at Error, same as before this distinction existed.
            if (status == StatusCodes.Status500InternalServerError)
                _logger.LogError(ex, "Unhandled exception on {Method} {Path}", context.Request.Method, context.Request.Path);
            else
                _logger.LogWarning(ex, "Request rejected ({Status} {Title}) on {Method} {Path}: {Reason}",
                    status, title, context.Request.Method, context.Request.Path, ex.Message);

            // If the response has already started streaming, there's no way to change its
            // status code or body anymore -- just let it propagate having already logged it.
            if (context.Response.HasStarted)
                throw;

            var problem = new ProblemDetails
            {
                Status = status,
                Title = title,
                // Never leak internal exception messages for an unclassified 500 -- for the
                // exception types above, the message is already a deliberate, user-facing
                // rejection reason (e.g. "Room is already reserved for this period.").
                Detail = status == StatusCodes.Status500InternalServerError ? null : ex.Message,
                Instance = context.Request.Path
            };

            // Deliberately not calling Response.Clear() here: it clears Headers too, which
            // would strip out anything earlier middleware already added to this response --
            // e.g. CORS's Access-Control-Allow-Origin, since UseCors runs "inside" this
            // middleware and adds its header before an exception from further downstream
            // propagates back up to this catch block.
            // WriteAsJsonAsync always sets its own Content-Type (defaulting to plain
            // application/json) unless told otherwise, so it has to be passed explicitly
            // here rather than relying on assigning context.Response.ContentType beforehand.
            context.Response.StatusCode = status;
            await context.Response.WriteAsJsonAsync(problem, options: null as System.Text.Json.JsonSerializerOptions, contentType: "application/problem+json");
        }
    }
}

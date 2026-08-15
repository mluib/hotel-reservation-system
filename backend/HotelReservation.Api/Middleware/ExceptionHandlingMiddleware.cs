using System.Net;
using System.Text.Json;

namespace HotelReservation.Api.Middleware;

// Minimal safety net: without this, exceptions that no controller/use case already
// catches (e.g. the double-booking check in CreateReservation) bubble all the way up
// as raw, unlogged 500s. Logs the exception and returns a generic error body instead.
// Deliberately not ProblemDetails-based -- standardizing error responses across the API
// is Phase 6 backlog ("global exception handling").
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
            _logger.LogError(ex, "Unhandled exception on {Method} {Path}", context.Request.Method, context.Request.Path);

            // If the response has already started streaming, there's no way to change its
            // status code or body anymore -- just let it propagate having already logged it.
            if (context.Response.HasStarted)
                throw;

            // Deliberately not calling Response.Clear() here: it clears Headers too, which
            // would strip out anything earlier middleware already added to this response --
            // e.g. CORS's Access-Control-Allow-Origin, since UseCors runs "inside" this
            // middleware and adds its header before an exception from further downstream
            // propagates back up to this catch block.
            context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsync(JsonSerializer.Serialize(new { error = "An unexpected error occurred." }));
        }
    }
}

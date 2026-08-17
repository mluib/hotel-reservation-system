import { HttpErrorResponse } from '@angular/common/http';

// The backend's error responses are ProblemDetails-shaped (RFC 7807), not the old
// { error: "message" } shape this used to expect -- see ExceptionHandlingMiddleware and
// [ApiController]'s own automatic validation responses. Two cases:
// - Thrown exceptions (NotFoundException, ConflictException, ...) produce
//   { title, status, detail, instance }, with the specific rejection reason in `detail`
//   (e.g. "Room is already reserved for this period.").
// - Automatic DataAnnotations failures produce a ValidationProblemDetails with no `detail`,
//   instead an `errors` dictionary of field name -> messages (e.g. weak-password
//   validation); the first message there is the useful one to surface.
export function extractErrorMessage(err: unknown, fallback = 'Something went wrong. Please try again.'): string {
  if (err instanceof HttpErrorResponse) {
    const body = err.error as
      | { detail?: unknown; title?: unknown; errors?: Record<string, string[]> }
      | null;

    if (body && typeof body.detail === 'string') return body.detail;

    const firstFieldError = body?.errors && Object.values(body.errors)[0]?.[0];
    if (firstFieldError) return firstFieldError;

    if (body && typeof body.title === 'string') return body.title;
  }
  return fallback;
}

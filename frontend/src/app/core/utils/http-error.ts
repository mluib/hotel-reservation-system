import { HttpErrorResponse } from '@angular/common/http';

// The backend's error responses are consistently shaped as { error: "message" }
// (see e.g. AccountController, RoomsController's delete-rejection). This pulls that
// message out, falling back to something generic for anything else (network errors,
// unhandled 500s, etc).
export function extractErrorMessage(err: unknown, fallback = 'Something went wrong. Please try again.'): string {
  if (err instanceof HttpErrorResponse) {
    const body = err.error as { error?: unknown } | null;
    if (body && typeof body.error === 'string') return body.error;
  }
  return fallback;
}

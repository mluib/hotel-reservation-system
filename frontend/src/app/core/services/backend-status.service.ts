import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { Injectable, inject, signal } from '@angular/core';
import { catchError, of } from 'rxjs';
import { environment } from '../../../environments/environment';

export type BackendStatus = 'checking' | 'available' | 'unavailable';

const HEALTH_URL = `${environment.apiBaseUrl}/health`;
const RETRY_INTERVAL_MS = 5000;

// Tracks whether the backend API is reachable at all, as distinct from an ordinary
// request failing (401 on a bad login, 404, a validation error, ...). The app root
// reads status() to decide whether to render the real app or a dedicated "backend
// unavailable" screen -- so a missing backend shows one honest message instead of an
// app that quietly renders empty/placeholder data and a misleading "invalid
// credentials" on login.
@Injectable({ providedIn: 'root' })
export class BackendStatusService {
  private readonly http = inject(HttpClient);

  readonly status = signal<BackendStatus>('checking');

  private pollHandle: ReturnType<typeof setInterval> | null = null;

  /** Pings the backend once and updates status(). Safe to call repeatedly (e.g. a
   * manual "Retry" click) -- it just re-runs the same check. */
  check(): void {
    this.http
      .get(HEALTH_URL)
      .pipe(catchError(() => of(null)))
      .subscribe((result) => {
        if (result) {
          this.status.set('available');
          this.stopPolling();
        } else {
          this.status.set('unavailable');
          this.startPolling();
        }
      });
  }

  /** Called by backendStatusInterceptor whenever any request fails with a
   * connectivity error, not just the health check -- catches the backend
   * disappearing mid-session (container stopped), not only at startup. */
  reportUnreachable(): void {
    if (this.status() === 'unavailable') return;
    this.status.set('unavailable');
    this.startPolling();
  }

  private startPolling(): void {
    if (this.pollHandle) return;
    this.pollHandle = setInterval(() => this.check(), RETRY_INTERVAL_MS);
  }

  private stopPolling(): void {
    if (!this.pollHandle) return;
    clearInterval(this.pollHandle);
    this.pollHandle = null;
  }
}

/** Status 0 is HttpClient's signal for "the request never reached a server" --
 * connection refused, DNS failure, or (indistinguishably) a CORS failure -- as
 * opposed to any status the backend itself returned. */
export function isConnectivityError(err: unknown): boolean {
  return err instanceof HttpErrorResponse && err.status === 0;
}

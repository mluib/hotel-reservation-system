import { Injectable, signal } from '@angular/core';

// A one-shot message shown as a dialog at the app root — currently used by
// roleGuard to explain *why* someone got redirected instead of bouncing them
// silently (e.g. an admin trying to book a room).
@Injectable({ providedIn: 'root' })
export class AppNoticeService {
  readonly message = signal<string | null>(null);

  show(message: string): void {
    this.message.set(message);
  }

  clear(): void {
    this.message.set(null);
  }
}

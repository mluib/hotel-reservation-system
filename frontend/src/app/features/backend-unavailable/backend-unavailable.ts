import { Component, inject } from '@angular/core';
import { BackendStatusService } from '../../core/services/backend-status.service';

// Full-page replacement for the entire app (see app.html) whenever the backend can't
// be reached -- shown instead of a real app rendered against absent data (empty
// lists, a misleading "invalid credentials" on login, ...).
@Component({
  selector: 'app-backend-unavailable',
  templateUrl: './backend-unavailable.html',
  styleUrl: './backend-unavailable.css',
})
export class BackendUnavailable {
  protected readonly backendStatus = inject(BackendStatusService);

  retry(): void {
    this.backendStatus.check();
  }
}

import { Component, OnInit, inject } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { Nav } from './layout/nav/nav';
import { ErrorDialog } from './shared/error-dialog/error-dialog';
import { AppNoticeService } from './core/services/app-notice.service';
import { BackendStatusService } from './core/services/backend-status.service';
import { BackendUnavailable } from './features/backend-unavailable/backend-unavailable';

@Component({
  selector: 'app-root',
  imports: [RouterOutlet, Nav, ErrorDialog, BackendUnavailable],
  templateUrl: './app.html',
  styleUrl: './app.css',
})
export class App implements OnInit {
  protected readonly notice = inject(AppNoticeService);
  protected readonly backendStatus = inject(BackendStatusService);

  ngOnInit(): void {
    this.backendStatus.check();
  }
}

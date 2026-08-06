import { Component, inject } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { Nav } from './layout/nav/nav';
import { ErrorDialog } from './shared/error-dialog/error-dialog';
import { AppNoticeService } from './core/services/app-notice.service';

@Component({
  selector: 'app-root',
  imports: [RouterOutlet, Nav, ErrorDialog],
  templateUrl: './app.html',
  styleUrl: './app.css',
})
export class App {
  protected readonly notice = inject(AppNoticeService);
}

import { Component, input, output } from '@angular/core';

// Shown for server-side rejections the user can't retry their way out of (e.g.
// "this room still has reservations"), matching the mockup's "Can't delete" dialog.
@Component({
  selector: 'app-error-dialog',
  templateUrl: './error-dialog.html',
})
export class ErrorDialog {
  readonly title = input<string>("Can't do that");
  readonly message = input.required<string>();
  readonly closed = output<void>();
}

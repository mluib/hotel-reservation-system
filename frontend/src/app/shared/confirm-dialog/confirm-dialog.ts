import { Component, input, output } from '@angular/core';

// Generic yes/no confirmation, reused wherever the mockup reuses its confirmDialog
// (cancel a reservation, delete a room/customer/reservation, etc). The parent owns
// whether it's shown and what "confirm" actually does — this component only renders.
@Component({
  selector: 'app-confirm-dialog',
  templateUrl: './confirm-dialog.html',
})
export class ConfirmDialog {
  readonly message = input.required<string>();
  readonly confirmed = output<void>();
  readonly cancelled = output<void>();
}

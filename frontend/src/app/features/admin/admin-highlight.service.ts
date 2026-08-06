import { Injectable, signal } from '@angular/core';

// Backs the "jump to customer/room" links on the Reservations tab: RoomsTab/
// CustomersTab read these to apply a brief highlight-fade on the matching row after
// the Reservations tab navigates there, matching the mockup's cross-tab jump.
@Injectable({ providedIn: 'root' })
export class AdminHighlightService {
  readonly highlightRoomId = signal<string | null>(null);
  readonly highlightCustomerId = signal<string | null>(null);

  private roomTimer?: ReturnType<typeof setTimeout>;
  private customerTimer?: ReturnType<typeof setTimeout>;

  jumpToRoom(roomId: string): void {
    this.highlightRoomId.set(roomId);
    clearTimeout(this.roomTimer);
    this.roomTimer = setTimeout(() => this.highlightRoomId.set(null), 1000);
  }

  jumpToCustomer(customerId: string): void {
    this.highlightCustomerId.set(customerId);
    clearTimeout(this.customerTimer);
    this.customerTimer = setTimeout(() => this.highlightCustomerId.set(null), 1000);
  }
}

import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { Router } from '@angular/router';
import { forkJoin } from 'rxjs';
import { ReservationsService } from '../../../core/services/reservations.service';
import { RoomsService } from '../../../core/services/rooms.service';
import { CustomersService } from '../../../core/services/customers.service';
import { AdminHighlightService } from '../admin-highlight.service';
import { Reservation } from '../../../core/models/reservation.model';
import { nightsBetween, reservationTotal } from '../../../core/utils/dates';
import { ConfirmDialog } from '../../../shared/confirm-dialog/confirm-dialog';

interface ReservationRow extends Reservation {
  customerName: string;
  roomNumber: string;
  nights: number;
  total: number;
}

type SortKey = 'customerName' | 'roomNumber' | 'checkIn' | 'checkOut' | 'nights' | 'total' | 'status';

@Component({
  selector: 'app-reservations-tab',
  imports: [ConfirmDialog],
  templateUrl: './reservations-tab.html',
})
export class ReservationsTab implements OnInit {
  private readonly reservationsService = inject(ReservationsService);
  private readonly roomsService = inject(RoomsService);
  private readonly customersService = inject(CustomersService);
  private readonly router = inject(Router);
  protected readonly highlight = inject(AdminHighlightService);

  private readonly rows = signal<ReservationRow[]>([]);
  private readonly sort = signal<{ key: SortKey; dir: 'asc' | 'desc' }>({
    key: 'checkIn',
    dir: 'desc',
  });

  protected readonly sortedRows = computed(() => {
    const { key, dir } = this.sort();
    return [...this.rows()].sort((a, b) => {
      const av = a[key];
      const bv = b[key];
      const cmp = typeof av === 'number' && typeof bv === 'number' ? av - bv : String(av).localeCompare(String(bv));
      return dir === 'asc' ? cmp : -cmp;
    });
  });

  protected readonly pendingCancel = signal<ReservationRow | null>(null);
  protected readonly pendingDelete = signal<ReservationRow | null>(null);

  ngOnInit(): void {
    this.load();
  }

  protected sortArrow(key: SortKey): string {
    const s = this.sort();
    return s.key !== key ? '' : s.dir === 'asc' ? '▲' : '▼';
  }

  protected toggleSort(key: SortKey): void {
    this.sort.update((s) => ({
      key,
      dir: s.key === key && s.dir === 'asc' ? 'desc' : 'asc',
    }));
  }

  protected jumpToCustomer(customerId: string): void {
    this.router.navigate(['/admin/customers']).then(() => this.highlight.jumpToCustomer(customerId));
  }

  protected jumpToRoom(roomId: string): void {
    this.router.navigate(['/admin/rooms']).then(() => this.highlight.jumpToRoom(roomId));
  }

  protected confirmCancel(): void {
    const row = this.pendingCancel();
    if (!row) return;
    this.reservationsService.cancel(row.id).subscribe(() => {
      this.pendingCancel.set(null);
      this.load();
    });
  }

  protected confirmDelete(): void {
    const row = this.pendingDelete();
    if (!row) return;
    this.reservationsService.delete(row.id).subscribe(() => {
      this.pendingDelete.set(null);
      this.load();
    });
  }

  private load(): void {
    forkJoin({
      reservations: this.reservationsService.getAll(),
      rooms: this.roomsService.getAll(),
      customers: this.customersService.getAll(),
    }).subscribe(({ reservations, rooms, customers }) => {
      const roomsById = new Map(rooms.map((r) => [r.id, r]));
      const customersById = new Map(customers.map((c) => [c.id, c]));

      this.rows.set(
        reservations.map((res) => {
          const room = roomsById.get(res.roomId);
          const customer = customersById.get(res.customerId);
          return {
            ...res,
            roomNumber: room?.number ?? '?',
            customerName: customer ? `${customer.firstName} ${customer.lastName}` : '?',
            nights: nightsBetween(res.checkIn, res.checkOut),
            total: reservationTotal(res.checkIn, res.checkOut, res.pricePerNight),
          };
        }),
      );
    });
  }
}

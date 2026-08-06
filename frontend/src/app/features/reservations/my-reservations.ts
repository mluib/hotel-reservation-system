import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { SlicePipe } from '@angular/common';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { forkJoin } from 'rxjs';
import { ReservationsService } from '../../core/services/reservations.service';
import { RoomsService } from '../../core/services/rooms.service';
import { Reservation } from '../../core/models/reservation.model';
import { Room } from '../../core/models/room.model';
import { reservationTotal } from '../../core/utils/dates';
import { ConfirmDialog } from '../../shared/confirm-dialog/confirm-dialog';

interface ReservationRow extends Reservation {
  roomNumber: string;
  roomType: string;
  total: number;
}

@Component({
  selector: 'app-my-reservations',
  imports: [RouterLink, ConfirmDialog, SlicePipe],
  templateUrl: './my-reservations.html',
  styleUrl: './my-reservations.css',
})
export class MyReservations implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly reservationsService = inject(ReservationsService);
  private readonly roomsService = inject(RoomsService);

  private readonly reservations = signal<Reservation[]>([]);
  private readonly rooms = signal<Room[]>([]);

  protected readonly bookingSuccess = signal(false);
  protected readonly pendingCancelId = signal<string | null>(null);

  protected readonly rows = computed<ReservationRow[]>(() => {
    const roomsById = new Map(this.rooms().map((r) => [r.id, r]));
    return this.reservations().map((res) => {
      const room = roomsById.get(res.roomId);
      return {
        ...res,
        roomNumber: room?.number ?? '?',
        roomType: room?.type ?? '?',
        total: reservationTotal(res.checkIn, res.checkOut, res.pricePerNight),
      };
    });
  });

  ngOnInit(): void {
    this.bookingSuccess.set(this.route.snapshot.queryParamMap.get('booked') === '1');
    this.load();
  }

  protected cancel(id: string): void {
    this.reservationsService.cancel(id).subscribe(() => {
      this.pendingCancelId.set(null);
      this.load();
    });
  }

  private load(): void {
    forkJoin({
      reservations: this.reservationsService.getMine(),
      rooms: this.roomsService.getAll(),
    }).subscribe(({ reservations, rooms }) => {
      this.reservations.set(reservations);
      this.rooms.set(rooms);
    });
  }
}

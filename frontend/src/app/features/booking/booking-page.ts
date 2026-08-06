import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { FormBuilder, ReactiveFormsModule } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { RoomsService } from '../../core/services/rooms.service';
import { ReservationsService } from '../../core/services/reservations.service';
import { Room } from '../../core/models/room.model';
import { checkOutAfterCheckIn } from '../../core/validators/date-range.validator';
import { extractErrorMessage } from '../../core/utils/http-error';
import { nightsBetween } from '../../core/utils/dates';

@Component({
  selector: 'app-booking-page',
  imports: [ReactiveFormsModule, RouterLink],
  templateUrl: './booking-page.html',
  styleUrl: './booking-page.css',
})
export class BookingPage implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly roomsService = inject(RoomsService);
  private readonly reservationsService = inject(ReservationsService);

  protected readonly room = signal<Room | null>(null);
  protected readonly submitting = signal(false);
  protected readonly serverError = signal<string | null>(null);

  protected readonly form = this.fb.nonNullable.group(
    { checkIn: [''], checkOut: [''] },
    { validators: checkOutAfterCheckIn() },
  );

  // computed() only reruns when a *signal* it read changes — FormGroup.getRawValue()
  // isn't one, so the total/nights below need the form's RxJS valueChanges bridged
  // into a signal first, or they'd freeze after their first evaluation.
  private readonly formValue = toSignal(this.form.valueChanges, {
    initialValue: this.form.getRawValue(),
  });

  protected readonly nights = computed(() => {
    const { checkIn, checkOut } = this.formValue();
    return checkIn && checkOut && checkOut > checkIn ? nightsBetween(checkIn, checkOut) : 0;
  });

  protected readonly total = computed(() => this.nights() * (this.room()?.pricePerNight ?? 0));

  // Recomputed on every change signal read, mirroring the mockup's inline
  // "N nights x $price/night" label.
  protected readonly nightsLabel = computed(() => {
    const n = this.nights();
    const price = this.room()?.pricePerNight ?? 0;
    return `${n} night${n === 1 ? '' : 's'} × $${price}/night`;
  });

  ngOnInit(): void {
    const roomId = this.route.snapshot.paramMap.get('roomId');
    if (!roomId) return;
    this.roomsService.getById(roomId).subscribe((room) => this.room.set(room));

    // Re-validate on every keystroke, not just on submit.
    this.form.valueChanges.subscribe(() => this.serverError.set(null));
  }

  protected confirmBooking(): void {
    const room = this.room();
    const { checkIn, checkOut } = this.form.getRawValue();
    if (!room) return;

    if (!checkIn || !checkOut) {
      this.serverError.set('Select both check-in and check-out dates.');
      return;
    }
    if (this.form.invalid) {
      this.serverError.set('Check-out must be after check-in.');
      return;
    }

    this.submitting.set(true);
    this.reservationsService.create({ roomId: room.id, checkIn, checkOut }).subscribe({
      next: () => this.router.navigate(['/reservations/mine'], { queryParams: { booked: 1 } }),
      error: (err) => {
        this.submitting.set(false);
        this.serverError.set(extractErrorMessage(err));
      },
    });
  }
}

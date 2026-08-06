import { Component, OnInit, inject, input, output, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { RoomsService } from '../../../../core/services/rooms.service';
import { HotelService } from '../../../../core/services/hotel.service';
import { ROOM_TYPES, Room } from '../../../../core/models/room.model';
import { extractErrorMessage } from '../../../../core/utils/http-error';
import { resolveImageUrl } from '../../../../core/utils/image-url';

@Component({
  selector: 'app-room-dialog',
  imports: [ReactiveFormsModule],
  templateUrl: './room-dialog.html',
})
export class RoomDialog implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly roomsService = inject(RoomsService);
  private readonly hotelService = inject(HotelService);

  readonly room = input<Room | null>(null);
  readonly saved = output<void>();
  readonly cancelled = output<void>();

  protected readonly roomTypes = ROOM_TYPES;
  protected readonly error = signal<string | null>(null);
  protected readonly submitting = signal(false);
  protected readonly uploading = signal(false);
  protected readonly resolveImageUrl = resolveImageUrl;

  protected readonly form = this.fb.nonNullable.group({
    number: ['', Validators.required],
    type: this.roomTypes[0],
    pricePerNight: ['', [Validators.required, Validators.min(0.01)]],
  });

  protected get isNew(): boolean {
    return this.room() === null;
  }

  protected get title(): string {
    return this.isNew ? 'Add room' : 'Edit room';
  }

  ngOnInit(): void {
    const room = this.room();
    if (room) {
      this.form.setValue({
        number: room.number,
        type: room.type,
        pricePerNight: String(room.pricePerNight),
      });
    }
  }

  protected save(): void {
    const price = Number(this.form.getRawValue().pricePerNight);
    if (this.form.invalid || !price || price <= 0) {
      this.error.set('Enter a room number and a valid price.');
      return;
    }

    const room = this.room();
    const hotelId = room?.hotelId ?? this.hotelService.hotel()?.id;
    if (!hotelId) {
      this.error.set('Hotel is not loaded yet — try again in a moment.');
      return;
    }

    const payload = {
      number: this.form.getRawValue().number,
      type: this.form.getRawValue().type as Room['type'],
      pricePerNight: price,
      hotelId,
    };

    this.submitting.set(true);
    const onError = (err: unknown) => {
      this.submitting.set(false);
      this.error.set(extractErrorMessage(err));
    };

    if (room) {
      this.roomsService.update(room.id, payload).subscribe({ next: () => this.saved.emit(), error: onError });
    } else {
      this.roomsService.create(payload).subscribe({ next: () => this.saved.emit(), error: onError });
    }
  }

  protected onFileSelected(event: Event): void {
    const room = this.room();
    const file = (event.target as HTMLInputElement).files?.[0];
    if (!room || !file) return;

    this.uploading.set(true);
    this.roomsService.uploadImage(room.id, file).subscribe({
      next: () => {
        this.uploading.set(false);
        this.saved.emit(); // let the parent refetch so the new photo shows everywhere
      },
      error: (err) => {
        this.uploading.set(false);
        this.error.set(extractErrorMessage(err));
      },
    });
  }
}

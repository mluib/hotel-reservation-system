import { Component, OnInit, computed, inject, input, output, signal } from '@angular/core';
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

  // A selected file is only staged/previewed here — the actual upload happens
  // together with Save, as one action, instead of firing immediately on selection
  // (which used to close the dialog early and discard any other unsaved edits).
  protected readonly pendingImageFile = signal<File | null>(null);

  protected readonly previewUrl = computed(() => {
    const file = this.pendingImageFile();
    if (file) return URL.createObjectURL(file);
    return resolveImageUrl(this.room()?.imageUrl, this.roomsService.imageVersion());
  });

  protected readonly form = this.fb.nonNullable.group({
    name: ['', Validators.required],
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
        name: room.name,
        number: room.number,
        type: room.type,
        pricePerNight: String(room.pricePerNight),
      });
    }
  }

  protected onFileSelected(event: Event): void {
    const file = (event.target as HTMLInputElement).files?.[0];
    if (file) this.pendingImageFile.set(file);
  }

  protected save(): void {
    const price = Number(this.form.getRawValue().pricePerNight);
    if (this.form.invalid || !price || price <= 0) {
      this.error.set('Enter a room name, number, and a valid price.');
      return;
    }

    const room = this.room();
    const hotelId = room?.hotelId ?? this.hotelService.hotel()?.id;
    if (!hotelId) {
      this.error.set('Hotel is not loaded yet — try again in a moment.');
      return;
    }

    const payload = {
      name: this.form.getRawValue().name,
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

    // Room number/type/price are saved first; if a photo was staged, it's uploaded
    // right after using whichever id now applies (the existing room's, or the one
    // just returned by create) — both happen as one "Save", not two separate steps.
    const finishWithId = (id: string) => {
      const file = this.pendingImageFile();
      if (!file) {
        this.saved.emit();
        return;
      }
      this.roomsService.uploadImage(id, file).subscribe({ next: () => this.saved.emit(), error: onError });
    };

    if (room) {
      this.roomsService.update(room.id, payload).subscribe({
        next: () => finishWithId(room.id),
        error: onError,
      });
    } else {
      this.roomsService.create(payload).subscribe({
        next: (created) => finishWithId(created.id),
        error: onError,
      });
    }
  }
}

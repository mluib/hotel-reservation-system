import { Component, computed, inject, signal } from '@angular/core';
import { toObservable, toSignal } from '@angular/core/rxjs-interop';
import { FormBuilder, ReactiveFormsModule } from '@angular/forms';
import { filter, take } from 'rxjs';
import { HotelService } from '../../../core/services/hotel.service';
import { Hotel } from '../../../core/models/hotel.model';
import { extractErrorMessage } from '../../../core/utils/http-error';
import { resolveImageUrl } from '../../../core/utils/image-url';

type LeaveResolver = (result: boolean) => void;

@Component({
  selector: 'app-hotel-tab',
  imports: [ReactiveFormsModule],
  templateUrl: './hotel-tab.html',
  styleUrl: './hotel-tab.css',
})
export class HotelTab {
  private readonly fb = inject(FormBuilder);
  private readonly hotelService = inject(HotelService);

  protected readonly savedFlash = signal(false);
  protected readonly error = signal<string | null>(null);
  protected readonly submitting = signal(false);

  // A selected file is only staged/previewed here — the actual upload happens
  // together with Save (see performSave below), as one action, not immediately on
  // selection.
  protected readonly pendingImageFile = signal<File | null>(null);

  protected readonly previewUrl = computed(() => {
    const file = this.pendingImageFile();
    if (file) return URL.createObjectURL(file);
    return resolveImageUrl(this.hotelService.hotel()?.imageUrl, this.hotelService.imageVersion());
  });

  // Set only while a router navigation away from this tab is waiting on the user's
  // answer in the unsaved-changes dialog below (see unsaved-hotel-changes.guard.ts).
  protected readonly leaveResolver = signal<LeaveResolver | null>(null);

  private readonly savedName = signal('');
  private readonly savedAddress = signal('');

  protected readonly form = this.fb.nonNullable.group({ name: [''], address: [''] });

  // See booking-page.ts for why valueChanges needs bridging into a signal before a
  // computed() can react to it.
  private readonly formValue = toSignal(this.form.valueChanges, {
    initialValue: this.form.getRawValue(),
  });

  // Public: unsaved-hotel-changes.guard.ts (outside this class) needs to read it.
  // A staged-but-not-yet-uploaded photo counts as unsaved too — leaving without
  // saving would silently drop it.
  readonly hasUnsavedChanges = computed(() => {
    const v = this.formValue();
    return (
      v.name !== this.savedName() || v.address !== this.savedAddress() || this.pendingImageFile() !== null
    );
  });

  constructor() {
    this.hotelService.ensureLoaded();

    // One-shot: seed the form once the shared hotel signal first resolves. A plain
    // subscribe callback (not an effect/computed) has no restriction on writing to
    // signals, which sidesteps needing this to re-run on every later signal change.
    toObservable(this.hotelService.hotel)
      .pipe(
        filter((h): h is Hotel => h !== null),
        take(1),
      )
      .subscribe((hotel) => {
        this.form.setValue({ name: hotel.name, address: hotel.address });
        this.savedName.set(hotel.name);
        this.savedAddress.set(hotel.address);
      });
  }

  protected onFileSelected(event: Event): void {
    const file = (event.target as HTMLInputElement).files?.[0];
    if (file) this.pendingImageFile.set(file);
  }

  protected save(): void {
    this.performSave(
      () => this.flashSaved(),
      (err) => this.error.set(extractErrorMessage(err)),
    );
  }

  /** Called by unsavedHotelChangesGuard; renders the 3-way dialog until resolved. */
  confirmLeave(resolve: LeaveResolver): void {
    this.leaveResolver.set(resolve);
  }

  protected keepEditing(): void {
    this.leaveResolver()?.(false);
    this.leaveResolver.set(null);
  }

  protected discard(): void {
    this.form.setValue({ name: this.savedName(), address: this.savedAddress() });
    this.pendingImageFile.set(null);
    this.leaveResolver()?.(true);
    this.leaveResolver.set(null);
  }

  protected saveAndContinue(): void {
    this.performSave(
      () => {
        this.flashSaved();
        this.leaveResolver()?.(true);
        this.leaveResolver.set(null);
      },
      (err) => {
        this.error.set(extractErrorMessage(err));
        this.leaveResolver()?.(false); // save failed — stay put rather than navigate away
        this.leaveResolver.set(null);
      },
    );
  }

  // Name/address are saved first; if a photo was staged, it's uploaded right after
  // — both as one "Save changes" action rather than two separate steps.
  private performSave(onSuccess: () => void, onError: (err: unknown) => void): void {
    const v = this.form.getRawValue();
    this.submitting.set(true);

    this.hotelService.update(v).subscribe({
      next: () => {
        this.savedName.set(v.name);
        this.savedAddress.set(v.address);

        const file = this.pendingImageFile();
        if (!file) {
          this.submitting.set(false);
          onSuccess();
          return;
        }

        this.hotelService.uploadImage(file).subscribe({
          next: () => {
            this.pendingImageFile.set(null);
            this.submitting.set(false);
            onSuccess();
          },
          error: (err) => {
            this.submitting.set(false);
            onError(err);
          },
        });
      },
      error: (err) => {
        this.submitting.set(false);
        onError(err);
      },
    });
  }

  private flashSaved(): void {
    this.savedFlash.set(true);
    setTimeout(() => this.savedFlash.set(false), 1800);
  }
}

import { Component, DestroyRef, OnInit, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormBuilder, ReactiveFormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { debounceTime } from 'rxjs';
import { RoomsService } from '../../core/services/rooms.service';
import { ROOM_TYPES, Room, RoomType } from '../../core/models/room.model';
import { checkOutAfterCheckIn } from '../../core/validators/date-range.validator';
import { resolveImageUrl } from '../../core/utils/image-url';

@Component({
  selector: 'app-rooms-page',
  imports: [ReactiveFormsModule],
  templateUrl: './rooms-page.html',
  styleUrl: './rooms-page.css',
})
export class RoomsPage implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly roomsService = inject(RoomsService);
  private readonly router = inject(Router);
  private readonly destroyRef = inject(DestroyRef);

  protected readonly roomTypes = ROOM_TYPES;
  protected readonly rooms = signal<Room[]>([]);

  protected readonly filterForm = this.fb.nonNullable.group(
    {
      checkIn: [''],
      checkOut: [''],
      type: ['all'],
      minPrice: [''],
      maxPrice: [''],
    },
    { validators: checkOutAfterCheckIn() },
  );

  ngOnInit(): void {
    this.fetch();
    this.filterForm.valueChanges
      .pipe(debounceTime(300), takeUntilDestroyed(this.destroyRef))
      .subscribe(() => {
        if (this.filterForm.valid) this.fetch();
      });
  }

  protected resolveImageUrl = resolveImageUrl;

  protected viewAndBook(room: Room): void {
    this.router.navigate(['/rooms', room.id, 'book']);
  }

  protected clearFilters(): void {
    this.filterForm.reset({ checkIn: '', checkOut: '', type: 'all', minPrice: '', maxPrice: '' });
  }

  private fetch(): void {
    const v = this.filterForm.getRawValue();
    this.roomsService
      .getAll({
        type: v.type === 'all' ? null : (v.type as RoomType),
        minPrice: v.minPrice ? Number(v.minPrice) : null,
        maxPrice: v.maxPrice ? Number(v.maxPrice) : null,
        checkIn: v.checkIn || null,
        checkOut: v.checkOut || null,
      })
      .subscribe((rooms) => this.rooms.set(rooms));
  }
}

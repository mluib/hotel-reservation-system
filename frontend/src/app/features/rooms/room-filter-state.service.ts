import { Injectable, signal } from '@angular/core';
import { RoomType } from '../../core/models/room.model';

export interface RoomFilterFormValue {
  checkIn: string;
  checkOut: string;
  type: RoomType | 'all';
  minPrice: string;
  maxPrice: string;
}

export const EMPTY_ROOM_FILTER: RoomFilterFormValue = {
  checkIn: '',
  checkOut: '',
  type: 'all',
  minPrice: '',
  maxPrice: '',
};

// Keeps the Rooms page's filter selections alive across navigation (e.g. going to
// book a room and back, or via the nav links) instead of resetting every time the
// page is recreated. Also lets the booking page prefill from whatever dates were
// already selected in the filter, instead of asking for them twice.
@Injectable({ providedIn: 'root' })
export class RoomFilterState {
  readonly value = signal<RoomFilterFormValue>(EMPTY_ROOM_FILTER);
}

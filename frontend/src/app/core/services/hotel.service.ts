import { HttpClient } from '@angular/common/http';
import { Injectable, inject, signal } from '@angular/core';
import { Observable, tap } from 'rxjs';
import { environment } from '../../../environments/environment';
import { Hotel, HotelUpdate } from '../models/hotel.model';

const BASE_URL = `${environment.apiBaseUrl}/hotel`;

@Injectable({ providedIn: 'root' })
export class HotelService {
  private readonly http = inject(HttpClient);

  // There's exactly one hotel record, read in many places (nav bar, Home, Admin's
  // Hotel tab) — kept as shared in-memory state instead of every reader re-fetching.
  readonly hotel = signal<Hotel | null>(null);
  private loaded = false;

  /** Fetches once and caches; safe to call from every component that needs it. */
  ensureLoaded(): void {
    if (this.loaded) return;
    this.loaded = true;
    this.http.get<Hotel>(BASE_URL).subscribe({
      next: (hotel) => this.hotel.set(hotel),
      error: () => (this.loaded = false), // allow a retry on the next ensureLoaded()
    });
  }

  update(request: HotelUpdate): Observable<void> {
    return this.http.put<void>(BASE_URL, request).pipe(tap(() => this.refresh()));
  }

  uploadImage(file: File): Observable<Hotel> {
    const formData = new FormData();
    formData.append('file', file);
    return this.http
      .post<Hotel>(`${BASE_URL}/image`, formData)
      .pipe(tap((hotel) => this.hotel.set(hotel)));
  }

  private refresh(): void {
    this.http.get<Hotel>(BASE_URL).subscribe((hotel) => this.hotel.set(hotel));
  }
}

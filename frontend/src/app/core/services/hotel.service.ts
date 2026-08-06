import { HttpClient } from '@angular/common/http';
import { Injectable, effect, inject, signal } from '@angular/core';
import { Title } from '@angular/platform-browser';
import { Observable, tap } from 'rxjs';
import { environment } from '../../../environments/environment';
import { Hotel, HotelUpdate } from '../models/hotel.model';

const BASE_URL = `${environment.apiBaseUrl}/hotel`;

@Injectable({ providedIn: 'root' })
export class HotelService {
  private readonly http = inject(HttpClient);
  private readonly title = inject(Title);

  // There's exactly one hotel record, read in many places (nav bar, Home, Admin's
  // Hotel tab) — kept as shared in-memory state instead of every reader re-fetching.
  readonly hotel = signal<Hotel | null>(null);

  // Bumped on every successful photo upload so <img> URLs can be cache-busted with a
  // query param — the server returns the same URL path before and after a re-upload
  // (the file is overwritten in place), so the browser would otherwise keep showing
  // the previous image bytes until a hard reload.
  readonly imageVersion = signal(0);

  private loaded = false;

  constructor() {
    effect(() => {
      const hotel = this.hotel();
      if (hotel) this.title.setTitle(hotel.name);
    });
  }

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
    return this.http.post<Hotel>(`${BASE_URL}/image`, formData).pipe(
      tap((hotel) => {
        this.hotel.set(hotel);
        this.imageVersion.update((v) => v + 1);
      }),
    );
  }

  private refresh(): void {
    this.http.get<Hotel>(BASE_URL).subscribe((hotel) => this.hotel.set(hotel));
  }
}

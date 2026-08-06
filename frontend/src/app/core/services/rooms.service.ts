import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject, signal } from '@angular/core';
import { Observable, tap } from 'rxjs';
import { environment } from '../../../environments/environment';
import { Room, RoomFilter, RoomUpsert } from '../models/room.model';

const BASE_URL = `${environment.apiBaseUrl}/rooms`;

@Injectable({ providedIn: 'root' })
export class RoomsService {
  private readonly http = inject(HttpClient);

  // Bumped on every successful photo upload — see resolveImageUrl() for why this is
  // needed to bust the browser's cache of a re-uploaded (same-path) image.
  readonly imageVersion = signal(0);

  getAll(filter?: RoomFilter): Observable<Room[]> {
    let params = new HttpParams();
    if (filter) {
      for (const [key, value] of Object.entries(filter)) {
        if (value !== null && value !== undefined && value !== '') {
          params = params.set(key, String(value));
        }
      }
    }
    return this.http.get<Room[]>(BASE_URL, { params });
  }

  getById(id: string): Observable<Room> {
    return this.http.get<Room>(`${BASE_URL}/${id}`);
  }

  create(request: RoomUpsert): Observable<Room> {
    return this.http.post<Room>(BASE_URL, request);
  }

  update(id: string, request: RoomUpsert): Observable<void> {
    return this.http.put<void>(`${BASE_URL}/${id}`, request);
  }

  delete(id: string): Observable<void> {
    return this.http.delete<void>(`${BASE_URL}/${id}`);
  }

  uploadImage(id: string, file: File): Observable<Room> {
    const formData = new FormData();
    formData.append('file', file);
    return this.http
      .post<Room>(`${BASE_URL}/${id}/image`, formData)
      .pipe(tap(() => this.imageVersion.update((v) => v + 1)));
  }
}

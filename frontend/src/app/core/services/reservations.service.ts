import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { CreateReservationRequest, Reservation } from '../models/reservation.model';

const BASE_URL = `${environment.apiBaseUrl}/reservations`;

@Injectable({ providedIn: 'root' })
export class ReservationsService {
  private readonly http = inject(HttpClient);

  /** Customer-role only. */
  create(request: CreateReservationRequest): Observable<Reservation> {
    return this.http.post<Reservation>(BASE_URL, request);
  }

  /** Customer-role only: the logged-in customer's own reservations. */
  getMine(): Observable<Reservation[]> {
    return this.http.get<Reservation[]>(`${BASE_URL}/mine`);
  }

  /** Customer (own) or Admin (any). */
  cancel(id: string): Observable<void> {
    return this.http.post<void>(`${BASE_URL}/${id}/cancel`, {});
  }

  /** Admin-only from here down. */
  getAll(): Observable<Reservation[]> {
    return this.http.get<Reservation[]>(BASE_URL);
  }

  delete(id: string): Observable<void> {
    return this.http.delete<void>(`${BASE_URL}/${id}`);
  }
}

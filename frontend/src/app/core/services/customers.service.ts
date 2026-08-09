import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { Customer, CustomerUpdate } from '../models/customer.model';

const BASE_URL = `${environment.apiBaseUrl}/customers`;

@Injectable({ providedIn: 'root' })
export class CustomersService {
  private readonly http = inject(HttpClient);

  /** Customer-role only: the logged-in customer's own profile. */
  getMe(): Observable<Customer> {
    return this.http.get<Customer>(`${BASE_URL}/me`);
  }

  /** Admin-only from here down. */
  getAll(): Observable<Customer[]> {
    return this.http.get<Customer[]>(BASE_URL);
  }

  getById(id: string): Observable<Customer> {
    return this.http.get<Customer>(`${BASE_URL}/${id}`);
  }

  update(id: string, request: CustomerUpdate): Observable<void> {
    return this.http.put<void>(`${BASE_URL}/${id}`, request);
  }

  delete(id: string): Observable<void> {
    return this.http.delete<void>(`${BASE_URL}/${id}`);
  }
}

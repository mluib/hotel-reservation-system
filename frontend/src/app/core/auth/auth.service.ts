import { Injectable, computed, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, tap } from 'rxjs';
import { environment } from '../../../environments/environment';
import {
  AuthenticationResponse,
  DecodedUser,
  LoginRequest,
  RegisterRequest,
  Role,
} from '../models/auth.model';
import { decodeToken, isExpired } from './jwt.util';

const STORAGE_KEY = 'hotel_auth_token';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly http = inject(HttpClient);
  private readonly tokenSignal = signal<string | null>(null);

  constructor() {
    const stored = localStorage.getItem(STORAGE_KEY);
    if (!stored) return;

    const decoded = decodeToken(stored);
    if (!decoded || isExpired(decoded)) {
      localStorage.removeItem(STORAGE_KEY);
    } else {
      this.tokenSignal.set(stored);
    }
  }

  readonly currentUser = computed<DecodedUser | null>(() => {
    const token = this.tokenSignal();
    if (!token) return null;
    const decoded = decodeToken(token);
    return decoded && !isExpired(decoded) ? decoded : null;
  });

  readonly isLoggedIn = computed(() => this.currentUser() !== null);
  readonly roles = computed<Role[]>(() => this.currentUser()?.roles ?? []);
  readonly isAdmin = computed(() => this.roles().includes('Admin'));
  readonly isCustomer = computed(() => this.roles().includes('Customer'));
  readonly userName = computed(() => this.currentUser()?.userName ?? '');

  login(request: LoginRequest): Observable<AuthenticationResponse> {
    return this.http
      .post<AuthenticationResponse>(`${environment.apiBaseUrl}/account/login`, request)
      .pipe(tap((res) => this.setToken(res.token)));
  }

  register(request: RegisterRequest): Observable<AuthenticationResponse> {
    return this.http
      .post<AuthenticationResponse>(`${environment.apiBaseUrl}/account/register`, request)
      .pipe(tap((res) => this.setToken(res.token)));
  }

  logout(): void {
    this.setToken(null);
  }

  /** Read synchronously by the auth interceptor to attach the Authorization header. */
  currentToken(): string | null {
    return this.tokenSignal();
  }

  private setToken(token: string | null): void {
    this.tokenSignal.set(token);
    if (token) {
      localStorage.setItem(STORAGE_KEY, token);
    } else {
      localStorage.removeItem(STORAGE_KEY);
    }
  }
}

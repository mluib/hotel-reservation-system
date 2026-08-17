import { HttpClient } from '@angular/common/http';
import { Injectable, effect, inject, signal } from '@angular/core';
import { environment } from '../../../environments/environment';
import { Customer } from '../models/customer.model';
import { AuthService } from '../auth/auth.service';

const BASE_URL = `${environment.apiBaseUrl}/customers/mine`;

// Keeps the logged-in customer's own profile (first/last name) around, mainly so
// the nav bar can show a real name instead of the JWT's bare email. Loads once per
// login and clears on logout, driven off AuthService's signals rather than requiring
// every consumer to remember to call it.
@Injectable({ providedIn: 'root' })
export class CustomerProfileService {
  private readonly http = inject(HttpClient);
  private readonly auth = inject(AuthService);

  readonly profile = signal<Customer | null>(null);
  private loadedForUserId: string | null = null;

  constructor() {
    effect(() => {
      const user = this.auth.currentUser();

      if (!user || !this.auth.isCustomer()) {
        this.profile.set(null);
        this.loadedForUserId = null;
        return;
      }

      if (this.loadedForUserId === user.userId) return;
      this.loadedForUserId = user.userId;

      this.http.get<Customer>(BASE_URL).subscribe({
        next: (customer) => this.profile.set(customer),
        error: () => (this.loadedForUserId = null),
      });
    });
  }
}

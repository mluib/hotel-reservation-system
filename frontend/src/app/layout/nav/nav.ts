import { Component, computed, inject, OnInit } from '@angular/core';
import { Router, RouterLink, RouterLinkActive } from '@angular/router';
import { AuthService } from '../../core/auth/auth.service';
import { CustomerProfileService } from '../../core/services/customer-profile.service';
import { HotelService } from '../../core/services/hotel.service';

@Component({
  selector: 'app-nav',
  imports: [RouterLink, RouterLinkActive],
  templateUrl: './nav.html',
  styleUrl: './nav.css',
})
export class Nav implements OnInit {
  private readonly router = inject(Router);
  protected readonly auth = inject(AuthService);
  protected readonly hotelService = inject(HotelService);
  private readonly customerProfile = inject(CustomerProfileService);

  protected readonly userLabel = computed(() => {
    if (this.auth.isAdmin()) return 'Admin';
    const profile = this.customerProfile.profile();
    return profile ? `${profile.firstName} ${profile.lastName}` : this.auth.userName();
  });

  ngOnInit(): void {
    this.hotelService.ensureLoaded();
  }

  logout(): void {
    this.auth.logout();
    this.router.navigate(['/']);
  }
}

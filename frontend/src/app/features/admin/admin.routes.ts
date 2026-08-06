import { Routes } from '@angular/router';
import { unsavedHotelChangesGuard } from './hotel-tab/unsaved-hotel-changes.guard';

export const adminRoutes: Routes = [
  {
    path: '',
    loadComponent: () => import('./admin-shell').then((m) => m.AdminShell),
    children: [
      { path: '', redirectTo: 'rooms', pathMatch: 'full' },
      {
        path: 'rooms',
        loadComponent: () => import('./rooms-tab/rooms-tab').then((m) => m.RoomsTab),
      },
      {
        path: 'reservations',
        loadComponent: () =>
          import('./reservations-tab/reservations-tab').then((m) => m.ReservationsTab),
      },
      {
        path: 'customers',
        loadComponent: () =>
          import('./customers-tab/customers-tab').then((m) => m.CustomersTab),
      },
      {
        path: 'hotel',
        loadComponent: () => import('./hotel-tab/hotel-tab').then((m) => m.HotelTab),
        canDeactivate: [unsavedHotelChangesGuard],
      },
    ],
  },
];

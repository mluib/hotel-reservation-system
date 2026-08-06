import { Routes } from '@angular/router';
import { roleGuard } from './core/auth/auth.guard';

export const routes: Routes = [
  {
    path: '',
    loadComponent: () => import('./features/home/home').then((m) => m.Home),
  },
  {
    path: 'auth',
    loadComponent: () => import('./features/auth/auth-page').then((m) => m.AuthPage),
  },
  {
    path: 'rooms',
    loadComponent: () => import('./features/rooms/rooms-page').then((m) => m.RoomsPage),
  },
  {
    path: 'rooms/:roomId/book',
    canActivate: [roleGuard('Customer')],
    loadComponent: () => import('./features/booking/booking-page').then((m) => m.BookingPage),
  },
  {
    path: 'reservations/mine',
    canActivate: [roleGuard('Customer')],
    loadComponent: () =>
      import('./features/reservations/my-reservations').then((m) => m.MyReservations),
  },
  {
    path: 'admin',
    canActivate: [roleGuard('Admin')],
    loadChildren: () => import('./features/admin/admin.routes').then((m) => m.adminRoutes),
  },
  { path: '**', redirectTo: '' },
];

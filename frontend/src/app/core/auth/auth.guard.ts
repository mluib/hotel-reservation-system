import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { AuthService } from './auth.service';
import { Role } from '../models/auth.model';

/** Requires any logged-in user; otherwise redirects to sign-in with a returnUrl. */
export const authGuard: CanActivateFn = (_route, state) => {
  const auth = inject(AuthService);
  const router = inject(Router);

  if (auth.isLoggedIn()) return true;

  return router.createUrlTree(['/auth'], { queryParams: { returnUrl: state.url } });
};

/** Requires a logged-in user with the given role. Not-logged-in goes to sign-in
 * (with returnUrl); logged in but wrong role goes home rather than looping back
 * to sign-in, since signing in again wouldn't change their role. */
export const roleGuard = (role: Role): CanActivateFn => {
  return (_route, state) => {
    const auth = inject(AuthService);
    const router = inject(Router);

    if (!auth.isLoggedIn()) {
      return router.createUrlTree(['/auth'], { queryParams: { returnUrl: state.url } });
    }

    if (auth.roles().includes(role)) return true;

    return router.createUrlTree(['/']);
  };
};

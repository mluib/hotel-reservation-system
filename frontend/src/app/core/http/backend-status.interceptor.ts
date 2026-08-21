import { HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { catchError, throwError } from 'rxjs';
import { BackendStatusService, isConnectivityError } from '../services/backend-status.service';

// Any request that fails with a connectivity error means the backend has gone away
// entirely, not that this particular request failed -- flip the shared status so the
// app root swaps to the "backend unavailable" screen instead of leaving whichever
// component made the call to fail on its own (silently, or with a misleading error).
export const backendStatusInterceptor: HttpInterceptorFn = (req, next) => {
  // inject() must run synchronously within this function call, not inside the
  // catchError callback below (which fires later, outside the injection context).
  const backendStatus = inject(BackendStatusService);

  return next(req).pipe(
    catchError((err: unknown) => {
      if (isConnectivityError(err)) {
        backendStatus.reportUnreachable();
      }
      return throwError(() => err);
    }),
  );
};

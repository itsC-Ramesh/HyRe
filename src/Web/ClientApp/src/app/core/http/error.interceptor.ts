import { HttpInterceptorFn, HttpErrorResponse } from '@angular/common/http';
import { inject } from '@angular/core';
import { catchError, from, switchMap, throwError } from 'rxjs';
import { AuthService } from '../auth/auth.service';
import { AuthState } from '../auth/auth.state';

export const errorInterceptor: HttpInterceptorFn = (req, next) => {
  const authService = inject(AuthService);
  const authState = inject(AuthState);

  return next(req).pipe(
    catchError((error: HttpErrorResponse) => {
      if (error.status === 401 && authState.accessToken()) {
        return from(authService.refreshToken()).pipe(
          switchMap((success) => {
            if (success) {
              const token = authState.accessToken();
              const cloned = req.clone({
                setHeaders: { Authorization: `Bearer ${token}` },
              });
              return next(cloned);
            }
            authService.logout();
            return throwError(() => error);
          })
        );
      }
      return throwError(() => error);
    })
  );
};

import { HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { AuthState } from '../auth/auth.state';

export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const authState = inject(AuthState);
  const token = authState.accessToken();

  // Skip auth header for refresh endpoint (token may be expired)
  if (token && req.url.includes('/api/') && !req.url.includes('/Auth/refresh')) {
    const cloned = req.clone({
      setHeaders: {
        Authorization: `Bearer ${token}`,
      },
    });
    return next(cloned);
  }

  return next(req);
};

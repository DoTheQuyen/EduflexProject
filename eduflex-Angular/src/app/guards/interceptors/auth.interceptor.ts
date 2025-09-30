import { HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { AuthHelperService } from '../../services/auth-helper.service';

export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const authHelper = inject(AuthHelperService);
  const authToken = authHelper.getAuthToken();
  
  // Skip adding auth token for login/register requests
  if (req.url.includes('/api/Auth/login') || req.url.includes('/api/Auth/register')) {
    return next(req);
  }
  
  if (authToken) {
    const authReq = req.clone({
      headers: req.headers.set('Authorization', `Bearer ${authToken}`)
    });
    return next(authReq);
  }
  
  return next(req);
};
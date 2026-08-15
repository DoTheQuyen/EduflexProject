import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { BehaviorSubject, catchError, filter, switchMap, take, throwError } from 'rxjs';
import { Client } from '@services/public.services';
import { AuthHelperService } from '../../services/auth-helper.service';

let isRefreshing = false;
const refreshDone$ = new BehaviorSubject<boolean>(true);

export const authInterceptor: HttpInterceptorFn = (req, next) => {
  // inject() must run synchronously here, in the interceptor's own injection
  // context — calling it later inside the catchError callback below would throw.
  const authClient = inject(Client);
  const authHelper = inject(AuthHelperService);
  const router = inject(Router);

  // Cookies carry the JWT/refresh token now, and the backend requires this header
  // on state-changing requests as a CSRF check — both apply to every call.
  const authReq = req.clone({
    withCredentials: true,
    headers: req.headers.set('X-Eduflex-Csrf', '1'),
  });

  const isAuthEndpoint =
    req.url.includes('/api/Auth/login') || req.url.includes('/api/Auth/refresh');

  if (isAuthEndpoint) {
    return next(authReq);
  }

  return next(authReq).pipe(
    catchError((error: HttpErrorResponse) => {
      if (error.status !== 401) {
        return throwError(() => error);
      }

      if (isRefreshing) {
        // A refresh triggered by another request is already in flight — wait for
        // it to finish, then retry this request instead of firing a second refresh.
        return refreshDone$.pipe(
          filter((done) => done),
          take(1),
          switchMap(() => next(authReq)),
        );
      }

      isRefreshing = true;
      refreshDone$.next(false);

      return authClient.refresh().pipe(
        switchMap(() => {
          isRefreshing = false;
          refreshDone$.next(true);
          return next(authReq);
        }),
        catchError((refreshError) => {
          isRefreshing = false;
          refreshDone$.next(true);
          authHelper.logout();
          router.navigate(['/login']);
          return throwError(() => refreshError);
        }),
      );
    }),
  );
};

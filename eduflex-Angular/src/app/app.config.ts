import { ApplicationConfig } from '@angular/core';
import { provideRouter } from '@angular/router';
import { provideHttpClient, withInterceptors, withFetch } from '@angular/common/http';
import { routes } from './app.routes';
import { authInterceptor } from '../app/guards/interceptors/auth.interceptor';
import { AUTH_API_BASE_URL } from '../app/services/public.services';
import { APPLICATION_API_BASE_URL } from '../app/services/api.services';
import { APPLICATION_API_BASE_URL as CONTENT_API_BASE_URL } from '../app/services/content.services';
import { environment } from './environments/environment';
import { provideAnimationsAsync } from '@angular/platform-browser/animations/async';

export const appConfig: ApplicationConfig = {
  providers: [
    provideRouter(routes),
    provideHttpClient(withInterceptors([authInterceptor]), withFetch()),
    provideAnimationsAsync(),
    { provide: APPLICATION_API_BASE_URL, useValue: environment.apiClientUrl },
    { provide: CONTENT_API_BASE_URL, useValue: environment.apiClientUrl },
    { provide: AUTH_API_BASE_URL, useValue: environment.publicApiUrl }
  ]
};
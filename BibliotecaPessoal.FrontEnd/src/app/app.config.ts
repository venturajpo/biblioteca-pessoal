import { ApplicationConfig, provideBrowserGlobalErrorListeners } from '@angular/core';
import { provideRouter } from '@angular/router';
import { provideHttpClient, withFetch, withInterceptors } from '@angular/common/http';
import { provideClientHydration } from '@angular/platform-browser';
import { routes } from './app.routes';
import { mockApiInterceptor } from './core/mock-api';

export const appConfig: ApplicationConfig = {
  providers: [
    provideBrowserGlobalErrorListeners(),
    provideRouter(routes),
    // Remova mockApiInterceptor quando houver back-end de verdade.
    provideHttpClient(withFetch(), withInterceptors([mockApiInterceptor])),
    provideClientHydration(),
  ],
};

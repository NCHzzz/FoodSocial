import { ApplicationConfig, provideBrowserGlobalErrorListeners } from '@angular/core';
import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { provideRouter } from '@angular/router';

import { routes } from './app.routes';
import { credentialsInterceptor } from './core/http/credentials.interceptor';
import { errorLoggingInterceptor } from './core/http/error-logging.interceptor';

export const appConfig: ApplicationConfig = {
  providers: [
    provideBrowserGlobalErrorListeners(),
    provideHttpClient(withInterceptors([credentialsInterceptor, errorLoggingInterceptor])),
    provideRouter(routes)
  ]
};

import { HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { DOCUMENT } from '@angular/common';
import { catchError, throwError } from 'rxjs';

export const errorLoggingInterceptor: HttpInterceptorFn = (req, next) => {
  const doc = inject(DOCUMENT);

  return next(req).pipe(
    catchError((error: unknown) => {
      if (typeof console !== 'undefined' && doc.defaultView) {
        console.error('[http]', req.method, req.urlWithParams, error);
      }

      return throwError(() => error);
    }),
  );
};

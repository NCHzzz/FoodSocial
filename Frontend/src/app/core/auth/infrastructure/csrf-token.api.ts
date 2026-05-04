import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable, catchError, map, shareReplay, tap, throwError } from 'rxjs';

export interface CsrfTokenResponse {
  requestToken: string;
  headerName: string;
}

@Injectable({ providedIn: 'root' })
export class CsrfTokenApi {
  private readonly httpClient = inject(HttpClient);

  private tokenRequest: Observable<HttpHeaders> | null = null;
  private csrfTokenRequest: Observable<CsrfTokenResponse> | null = null;

  private static readonly csrfEndpoint = '/api/auth/csrf';

  withCsrfHeader(): Observable<{ headers: HttpHeaders; withCredentials: true }> {
    return this.resolveHeaders().pipe(
      map((headers) => ({
        headers,
        withCredentials: true as const,
      })),
    );
  }

  private resolveHeaders(): Observable<HttpHeaders> {
    if (this.tokenRequest === null) {
      this.tokenRequest = this.getToken().pipe(
        map((token) => new HttpHeaders().set(token.headerName, token.requestToken)),
        catchError((error: unknown) => {
          this.tokenRequest = null;

          return throwError(() => error);
        }),
      );
    }

    return this.tokenRequest;
  }

  getToken(): Observable<CsrfTokenResponse> {
    if (this.csrfTokenRequest === null) {
      this.csrfTokenRequest = this.httpClient
        .get<CsrfTokenResponse>(CsrfTokenApi.csrfEndpoint, { withCredentials: true })
        .pipe(
          map((token) => this.validateToken(token)),
          tap(() => {
            this.csrfTokenRequest = null;
            this.tokenRequest = null;
          }),
          catchError((error: unknown) => {
            this.csrfTokenRequest = null;
            this.tokenRequest = null;

            return throwError(() => error);
          }),
          shareReplay({ bufferSize: 1, refCount: false }),
        );
    }

    return this.csrfTokenRequest;
  }

  private validateToken(token: CsrfTokenResponse): CsrfTokenResponse {
    if (token.headerName.trim().length === 0 || token.requestToken.trim().length === 0) {
      throw new Error('Invalid CSRF token response.');
    }

    return token;
  }
}

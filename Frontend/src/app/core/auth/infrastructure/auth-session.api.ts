import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { catchError, map, Observable, of } from 'rxjs';

import {
  AuthSessionState,
  CurrentUserResponse,
  createGuestAuthSessionState,
} from '../auth-session.models';

@Injectable({ providedIn: 'root' })
export class AuthSessionApi {
  private readonly httpClient = inject(HttpClient);

  private static readonly sessionEndpoint = '/api/auth/session';

  loadSessionState(): Observable<AuthSessionState> {
    return this.httpClient
      .get<CurrentUserResponse>(AuthSessionApi.sessionEndpoint, { withCredentials: true })
      .pipe(
        map((session): AuthSessionState => ({
          status: 'authenticated',
          session,
          isLoggingOut: false,
        })),
        catchError(() => of(createGuestAuthSessionState())),
      );
  }
}

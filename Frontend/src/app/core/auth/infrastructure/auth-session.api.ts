import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { catchError, map, Observable, of } from 'rxjs';

import {
  AuthSessionState,
  CurrentUserResponse,
  createGuestAuthSessionState,
  createUnavailableAuthSessionState,
} from '../auth-session.models';

@Injectable({ providedIn: 'root' })
export class AuthSessionApi {
  private readonly httpClient = inject(HttpClient);

  private static readonly sessionEndpoint = '/api/auth/session';

  loadSessionState(): Observable<AuthSessionState> {
    return this.httpClient
      .get<CurrentUserResponse>(AuthSessionApi.sessionEndpoint)
      .pipe(
        map((session): AuthSessionState => ({
          loadStatus: 'authenticated',
          session,
          isLoggingOut: false,
          lastLoadFailed: false,
        })),
        catchError((error) => of(error.status === 401 ? createGuestAuthSessionState() : createUnavailableAuthSessionState())),
      );
  }
}

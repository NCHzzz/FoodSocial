import { Injectable, inject, signal } from '@angular/core';
import { firstValueFrom } from 'rxjs';

import { BROWSER_WINDOW } from './browser-window.token';
import {
  AuthSessionState,
  createLoadingAuthSessionState,
} from './auth-session.models';
import { AuthSessionApi } from './infrastructure/auth-session.api';

@Injectable({ providedIn: 'root' })
export class AuthSessionService {
  private readonly authSessionApi = inject(AuthSessionApi);
  private readonly browserWindow = inject(BROWSER_WINDOW);
  private sessionLoadPromise: Promise<AuthSessionState> | null = null;

  private static readonly loginEndpoint = '/api/auth/login';
  private static readonly logoutEndpoint = '/api/auth/logout';

  private readonly authSessionState = signal<AuthSessionState>(createLoadingAuthSessionState());

  readonly authState = this.authSessionState.asReadonly();

  loadSession(): Promise<AuthSessionState> {
    this.authSessionState.set(createLoadingAuthSessionState());

    const sessionLoadPromise: Promise<AuthSessionState> = firstValueFrom(
      this.authSessionApi.loadSessionState(),
    ).then((nextState) => {
      this.authSessionState.set(nextState);
      this.sessionLoadPromise = null;

      return nextState;
    }).catch((error: unknown) => {
      this.sessionLoadPromise = null;
      throw error;
    });

    this.sessionLoadPromise = sessionLoadPromise;

    return sessionLoadPromise;
  }

  ensureSession(): Promise<AuthSessionState> {
    if (this.sessionLoadPromise !== null) {
      return this.sessionLoadPromise;
    }

    const currentState = this.authSessionState();

    if (currentState.loadStatus !== 'loading') {
      return Promise.resolve(currentState);
    }

    return this.loadSession();
  }

  startLogin(): void {
    this.browserWindow.location.assign(this.buildLoginUrl());
  }

  markLogoutPending(): void {
    this.authSessionState.update((currentState) => ({
      ...currentState,
      isLoggingOut: true,
    }));
  }

  buildLogoutUrl(): string {
    return `${AuthSessionService.logoutEndpoint}?returnUrl=${encodeURIComponent(this.resolveReturnUrl())}`;
  }

  private buildLoginUrl(): string {
    return `${AuthSessionService.loginEndpoint}?returnUrl=${encodeURIComponent(this.resolveReturnUrl())}`;
  }

  private resolveReturnUrl(): string {
    const { pathname, search, hash } = this.browserWindow.location;
    const path = pathname && pathname.startsWith('/') ? pathname : '/';

    return `${path}${search ?? ''}${hash ?? ''}`;
  }
}

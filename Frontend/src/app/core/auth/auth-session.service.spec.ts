import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { vi } from 'vitest';

import { BROWSER_WINDOW } from './browser-window.token';
import { CurrentUserResponse } from './auth-session.models';
import { AuthSessionService } from './auth-session.service';
import { AuthSessionApi } from './infrastructure/auth-session.api';

describe('AuthSessionService', () => {
  const locationAssign = vi.fn();
  const browserWindow = {
    location: {
      pathname: '/',
      search: '',
      hash: '',
      assign: locationAssign,
    },
  } as unknown as Window;

  const authenticatedSession: CurrentUserResponse = {
    user: {
      subject: 'subject-123',
      preferredUsername: 'demo-user',
      email: 'demo.user@foodsocial.local',
      roles: ['user'],
    },
    identity: {
      identityId: '6f4ad594-e916-4f0c-9d08-b1043e15db4b',
      state: 'Active',
      bootstrapPending: false,
      downstreamBootstrapPending: false,
    },
    serverTimeUtc: '2026-04-13T03:00:00Z',
  };

  let httpTestingController: HttpTestingController;
  let authSessionApi: AuthSessionApi;
  let service: AuthSessionService;

  beforeEach(() => {
    locationAssign.mockReset();
    browserWindow.location.pathname = '/';
    browserWindow.location.search = '';
    browserWindow.location.hash = '';

    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        { provide: BROWSER_WINDOW, useValue: browserWindow },
      ],
    });

    httpTestingController = TestBed.inject(HttpTestingController);
    authSessionApi = TestBed.inject(AuthSessionApi);
    service = TestBed.inject(AuthSessionService);
  });

  afterEach(() => {
    httpTestingController.verify();
  });

  it('loads the authenticated session from the BFF session endpoint', async () => {
    const sessionLoadPromise = service.loadSession();

    const request = httpTestingController.expectOne('/api/auth/session');
    expect(request.request.method).toBe('GET');
    expect(request.request.withCredentials).toBe(true);

    request.flush(authenticatedSession);

    await expect(sessionLoadPromise).resolves.toEqual({
      status: 'authenticated',
      session: authenticatedSession,
      isLoggingOut: false,
    });

    expect(service.state()).toEqual({
      status: 'authenticated',
      session: authenticatedSession,
      isLoggingOut: false,
    });
  });

  it('falls back to guest state when the session request fails', async () => {
    const sessionLoadPromise = service.loadSession();

    const request = httpTestingController.expectOne('/api/auth/session');
    request.flush('unauthenticated', { status: 401, statusText: 'Unauthorized' });

    await expect(sessionLoadPromise).resolves.toEqual({
      status: 'guest',
      session: null,
      isLoggingOut: false,
    });

    expect(service.state()).toEqual({
      status: 'guest',
      session: null,
      isLoggingOut: false,
    });
  });

  it('navigates the browser to the BFF login endpoint with the current relative return URL', () => {
    browserWindow.location.pathname = '/discover';
    browserWindow.location.search = '?tab=nearby';
    browserWindow.location.hash = '#pho';

    service.startLogin();

    expect(locationAssign).toHaveBeenCalledWith(
      '/api/auth/login?returnUrl=%2Fdiscover%3Ftab%3Dnearby%23pho',
    );
  });

  it('builds the logout URL with the current relative return URL', () => {
    browserWindow.location.pathname = '/profile';
    browserWindow.location.search = '?view=saved';

    expect(service.buildLogoutUrl()).toBe('/api/auth/logout?returnUrl=%2Fprofile%3Fview%3Dsaved');
  });

  it('reuses the active session request when a route guard asks for auth state during bootstrap', async () => {
    const firstRequest = service.loadSession();
    const ensuredRequest = service.ensureSession();

    const request = httpTestingController.expectOne('/api/auth/session');
    request.flush(authenticatedSession);

    await expect(firstRequest).resolves.toEqual({
      status: 'authenticated',
      session: authenticatedSession,
      isLoggingOut: false,
    });
    await expect(ensuredRequest).resolves.toEqual({
      status: 'authenticated',
      session: authenticatedSession,
      isLoggingOut: false,
    });
  });

  it('keeps raw session transport in the auth infrastructure client', () => {
    expect(authSessionApi).toBeTruthy();
  });
});

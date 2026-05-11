import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { credentialsInterceptor } from '../http/credentials.interceptor';
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
    document,
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
        provideHttpClient(withInterceptors([credentialsInterceptor])),
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
      loadStatus: 'authenticated',
      session: authenticatedSession,
      isLoggingOut: false,
      lastLoadFailed: false,
    });

    expect(service.authState()).toEqual({
      loadStatus: 'authenticated',
      session: authenticatedSession,
      isLoggingOut: false,
      lastLoadFailed: false,
    });
  });

  it('falls back to guest state when the session request fails', async () => {
    const sessionLoadPromise = service.loadSession();

    const request = httpTestingController.expectOne('/api/auth/session');
    request.flush('unauthenticated', { status: 401, statusText: 'Unauthorized' });

    await expect(sessionLoadPromise).resolves.toEqual({
      loadStatus: 'guest',
      session: null,
      isLoggingOut: false,
      lastLoadFailed: false,
    });

    expect(service.authState()).toEqual({
      loadStatus: 'guest',
      session: null,
      isLoggingOut: false,
      lastLoadFailed: false,
    });
  });

  it('keeps non-auth session failures explicit instead of collapsing them into guest state', async () => {
    const sessionLoadPromise = service.loadSession();

    const request = httpTestingController.expectOne('/api/auth/session');
    request.flush('downstream failure', { status: 503, statusText: 'Service Unavailable' });

    await expect(sessionLoadPromise).resolves.toEqual({
      loadStatus: 'unavailable',
      session: null,
      isLoggingOut: false,
      lastLoadFailed: true,
    });

    expect(service.authState()).toEqual({
      loadStatus: 'unavailable',
      session: null,
      isLoggingOut: false,
      lastLoadFailed: true,
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

  it('submits logout as a CSRF-protected POST form with the current relative return URL', async () => {
    browserWindow.location.pathname = '/profile';
    browserWindow.location.search = '?view=saved';
    const submit = vi.fn();
    const formElement = document.createElement('form');
    formElement.submit = submit;
    const originalCreateElement = document.createElement.bind(document);
    const createElementSpy = vi.spyOn(document, 'createElement');
    createElementSpy.mockImplementation((tagName: string) => {
      if (tagName === 'form') {
        return formElement;
      }

      return originalCreateElement(tagName);
    });

    const logoutPromise = service.submitLogout();
    expect(service.authState().isLoggingOut).toBe(true);

    const csrfRequest = httpTestingController.expectOne('/api/auth/csrf');
    expect(csrfRequest.request.method).toBe('GET');
    expect(csrfRequest.request.withCredentials).toBe(true);
    csrfRequest.flush({ requestToken: 'csrf-token', headerName: 'X-XSRF-TOKEN' });

    try {
      await logoutPromise;

      expect(formElement.method).toBe('post');
      expect(formElement.action).toContain('/api/auth/logout?returnUrl=%2Fprofile%3Fview%3Dsaved');
      expect((formElement.querySelector('input[name="__RequestVerificationToken"]') as HTMLInputElement).value).toBe('csrf-token');
      expect(submit).toHaveBeenCalledOnce();
    } finally {
      createElementSpy.mockRestore();
    }
  });

  it('clears logout pending state when CSRF token retrieval fails', async () => {
    const logoutPromise = service.submitLogout();
    expect(service.authState().isLoggingOut).toBe(true);

    const csrfRequest = httpTestingController.expectOne('/api/auth/csrf');
    csrfRequest.flush('csrf unavailable', { status: 503, statusText: 'Service Unavailable' });

    await expect(logoutPromise).rejects.toBeTruthy();
    expect(service.authState().isLoggingOut).toBe(false);
  });

  it('reuses the active session request when a route guard asks for auth state during bootstrap', async () => {
    const firstRequest = service.loadSession();
    const ensuredRequest = service.ensureSession();

    const request = httpTestingController.expectOne('/api/auth/session');
    request.flush(authenticatedSession);

    await expect(firstRequest).resolves.toEqual({
      loadStatus: 'authenticated',
      session: authenticatedSession,
      isLoggingOut: false,
      lastLoadFailed: false,
    });
    await expect(ensuredRequest).resolves.toEqual({
      loadStatus: 'authenticated',
      session: authenticatedSession,
      isLoggingOut: false,
      lastLoadFailed: false,
    });
  });

  it('keeps raw session transport in the auth infrastructure client', () => {
    expect(authSessionApi).toBeTruthy();
  });
});

import { TestBed } from '@angular/core/testing';
import { signal } from '@angular/core';
import { provideRouter } from '@angular/router';
import { vi } from 'vitest';

import { AuthSessionService } from '../core/auth/auth-session.service';
import { AuthSessionState } from '../core/auth/auth-session.models';
import { AppShellComponent } from './app-shell.component';

describe('AppShellComponent', () => {
  const createAuthSessionServiceMock = (state: AuthSessionState) => ({
    authState: signal(state),
    loadSession: vi.fn().mockResolvedValue(state),
    ensureSession: vi.fn().mockResolvedValue(state),
    startLogin: vi.fn(),
    submitLogout: vi.fn().mockResolvedValue(undefined),
  });

  let authSessionServiceMock = createAuthSessionServiceMock({
    loadStatus: 'guest',
    session: null,
    isLoggingOut: false,
    lastLoadFailed: false,
  });

  beforeEach(async () => {
    authSessionServiceMock = createAuthSessionServiceMock({
      loadStatus: 'guest',
      session: null,
      isLoggingOut: false,
      lastLoadFailed: false,
    });

    await TestBed.configureTestingModule({
      imports: [AppShellComponent],
      providers: [
        provideRouter([]),
        { provide: AuthSessionService, useValue: authSessionServiceMock },
      ],
    }).compileComponents();
  });

  it('should render the guest-safe public navigation and login entry', async () => {
    const fixture = TestBed.createComponent(AppShellComponent);
    await fixture.whenStable();

    const compiled = fixture.nativeElement as HTMLElement;

    expect(compiled.textContent).toContain('FoodSocial');
    expect(compiled.textContent).toContain('Discover');
    expect(compiled.textContent).toContain('Posts');
    expect(compiled.textContent).toContain('Profiles');
    expect(compiled.textContent).not.toContain('Create');
    expect(compiled.textContent).not.toContain('Notifications');
    expect(compiled.textContent).toContain('Log in');
  });

  it('should render authenticated navigation and logout entry for signed-in users', async () => {
    authSessionServiceMock = createAuthSessionServiceMock({
      loadStatus: 'authenticated',
      session: {
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
      },
      isLoggingOut: false,
      lastLoadFailed: false,
    });

    await TestBed.resetTestingModule()
      .configureTestingModule({
        imports: [AppShellComponent],
        providers: [
          provideRouter([]),
          { provide: AuthSessionService, useValue: authSessionServiceMock },
        ],
      })
      .compileComponents();

    const fixture = TestBed.createComponent(AppShellComponent);
    await fixture.whenStable();

    const compiled = fixture.nativeElement as HTMLElement;

    expect(compiled.textContent).toContain('Following');
    expect(compiled.textContent).toContain('Create');
    expect(compiled.textContent).toContain('Notifications');
    expect(compiled.textContent).toContain('Profile');
    expect(compiled.textContent).toContain('Log out');
  });
});

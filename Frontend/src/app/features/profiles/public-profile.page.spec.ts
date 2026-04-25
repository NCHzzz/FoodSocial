import { signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { ActivatedRoute } from '@angular/router';
import { Observable, of, throwError } from 'rxjs';

import { AuthSessionState } from '../../core/auth/auth-session.models';
import { AuthSessionService } from '../../core/auth/auth-session.service';
import { UserProfileApi } from '../../core/user-profiles/infrastructure/user-profile.api';
import {
  FollowUserResponse,
  UserProfileListResponse,
  UserProfileResponse,
  UserSavedItemsResponse,
} from '../../core/user-profiles/user-profile.models';
import { PublicProfilePage } from './public-profile.page';

describe('PublicProfilePage', () => {
  const identityId = '6f4ad594-e916-4f0c-9d08-b1043e15db4b';
  const profile: UserProfileResponse = {
    identityId,
    displayName: 'Demo Food Explorer',
    avatarUrl: null,
    bio: 'Chasing great com tam and pho spots.',
    preferredCuisines: ['Vietnamese', 'Street food'],
    dislikedIngredients: ['Cilantro'],
    dietaryRestrictions: [],
    followerCount: 12,
    followingCount: 8,
  };
  const followers: UserProfileListResponse = {
    totalCount: 1,
    items: [
      {
        identityId: 'follower-1',
        displayName: 'Pho Friend',
        avatarUrl: null,
      },
    ],
  };
  const following: UserProfileListResponse = {
    totalCount: 1,
    items: [
      {
        identityId: 'following-1',
        displayName: 'Banh Mi Guide',
        avatarUrl: null,
      },
    ],
  };
  const savedItems: UserSavedItemsResponse = {
    posts: [{ postId: 'post-1', savedAtUtc: '2026-04-25T00:00:00Z' }],
    dishes: [{ dishId: 'dish-1', savedAtUtc: '2026-04-25T00:00:00Z' }],
    places: [],
  };
  const authenticatedState: AuthSessionState = {
    loadStatus: 'authenticated',
    session: {
      user: {
        subject: 'subject-1',
        preferredUsername: 'demo',
        email: 'demo@example.com',
        roles: ['user'],
      },
      identity: {
        identityId: 'viewer-identity-id',
        state: 'Active',
        bootstrapPending: false,
        downstreamBootstrapPending: false,
      },
      serverTimeUtc: '2026-04-25T00:00:00Z',
    },
    isLoggingOut: false,
    lastLoadFailed: false,
  };
  const ownerState: AuthSessionState = {
    ...authenticatedState,
    session: authenticatedState.session
      ? {
          ...authenticatedState.session,
          identity: {
            ...authenticatedState.session.identity,
            identityId,
          },
        }
      : null,
  };
  const guestState: AuthSessionState = {
    loadStatus: 'guest',
    session: null,
    isLoggingOut: false,
    lastLoadFailed: false,
  };
  const loadingState: AuthSessionState = {
    loadStatus: 'loading',
    session: null,
    isLoggingOut: false,
    lastLoadFailed: false,
  };

  const configurePage = async (options?: {
    authState?: AuthSessionState;
    getProfile?: () => Observable<UserProfileResponse>;
    getFollowers?: () => Observable<UserProfileListResponse>;
    getFollowing?: () => Observable<UserProfileListResponse>;
    getSavedItems?: () => Observable<UserSavedItemsResponse>;
    follow?: () => Observable<FollowUserResponse>;
    unfollow?: () => Observable<FollowUserResponse>;
  }) => {
    const followCalls: string[] = [];
    const loginCalls: string[] = [];
    const savedCalls: string[] = [];
    const authStateSignal = signal(options?.authState ?? authenticatedState);

    await TestBed.configureTestingModule({
      imports: [PublicProfilePage],
      providers: [
        {
          provide: ActivatedRoute,
          useValue: {
            snapshot: {
              paramMap: {
                get: (name: string) => name === 'identityId' ? identityId : null,
              },
            },
          },
        },
        {
          provide: AuthSessionService,
          useValue: {
            authState: authStateSignal,
            startLogin: () => loginCalls.push('login'),
          },
        },
        {
          provide: UserProfileApi,
          useValue: {
            getProfile: () => (options?.getProfile ?? (() => of(profile)))(),
            getFollowers: () => (options?.getFollowers ?? (() => of(followers)))(),
            getFollowing: () => (options?.getFollowing ?? (() => of(following)))(),
            getSavedItems: () => {
              savedCalls.push('saved');
              return (options?.getSavedItems ?? (() => of(savedItems)))();
            },
            follow: (targetIdentityId: string) => {
              followCalls.push(`follow:${targetIdentityId}`);
              return (options?.follow ?? (() => of({ targetIdentityId, following: true })))();
            },
            unfollow: (targetIdentityId: string) => {
              followCalls.push(`unfollow:${targetIdentityId}`);
              return (options?.unfollow ?? (() => of({ targetIdentityId, following: false })))();
            },
          },
        },
      ],
    }).compileComponents();

    return { followCalls, loginCalls, savedCalls, authStateSignal };
  };

  afterEach(() => {
    TestBed.resetTestingModule();
  });

  it('loads profile identity and follow graph lists without leaking private saved state', async () => {
    const { savedCalls } = await configurePage();

    const fixture = TestBed.createComponent(PublicProfilePage);
    await fixture.whenStable();
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;

    expect(compiled.textContent).toContain('Demo Food Explorer');
    expect(compiled.textContent).toContain('12 followers');
    expect(compiled.textContent).toContain('8 following');
    expect(compiled.textContent).toContain('Vietnamese');
    expect(compiled.textContent).toContain('Cilantro');
    expect(compiled.textContent).toContain('Pho Friend');
    expect(compiled.textContent).toContain('Banh Mi Guide');
    expect(compiled.textContent).toContain('Saved posts, dishes, and places are private');
    expect(savedCalls).toEqual([]);
  });

  it('loads saved state only for the owner viewing their own public profile', async () => {
    const { savedCalls } = await configurePage({ authState: ownerState });

    const fixture = TestBed.createComponent(PublicProfilePage);
    await fixture.whenStable();
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;

    expect(compiled.textContent).toContain('1 saved posts');
    expect(compiled.textContent).toContain('1 saved dishes');
    expect(compiled.textContent).toContain('0 saved places');
    expect(savedCalls).toEqual(['saved']);
  });

  it('loads owner saved state when the public profile session hydrates after construction', async () => {
    const { authStateSignal, savedCalls } = await configurePage({ authState: loadingState });

    const fixture = TestBed.createComponent(PublicProfilePage);
    await fixture.whenStable();
    fixture.detectChanges();

    expect(savedCalls).toEqual([]);

    authStateSignal.set(ownerState);
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;

    expect(compiled.textContent).toContain('1 saved posts');
    expect(savedCalls).toEqual(['saved']);
  });

  it('shows gated follow action for guests and starts login', async () => {
    const { loginCalls } = await configurePage({ authState: guestState });

    const fixture = TestBed.createComponent(PublicProfilePage);
    await fixture.whenStable();
    fixture.detectChanges();

    const button = Array.from((fixture.nativeElement as HTMLElement).querySelectorAll('button'))
      .find((candidate) => candidate.textContent?.includes('Log in to follow'));
    button?.dispatchEvent(new Event('click'));
    fixture.detectChanges();

    expect(loginCalls).toEqual(['login']);
    expect((fixture.nativeElement as HTMLElement).textContent).toContain('Following is available after login');
  });

  it('follows and then unfollows an authenticated public profile', async () => {
    const { followCalls } = await configurePage();

    const fixture = TestBed.createComponent(PublicProfilePage);
    await fixture.whenStable();
    fixture.detectChanges();

    const firstButton = Array.from((fixture.nativeElement as HTMLElement).querySelectorAll('button'))
      .find((candidate) => candidate.textContent?.includes('Follow'));
    firstButton?.dispatchEvent(new Event('click'));
    await fixture.whenStable();
    fixture.detectChanges();

    const secondButton = Array.from((fixture.nativeElement as HTMLElement).querySelectorAll('button'))
      .find((candidate) => candidate.textContent?.includes('Unfollow'));
    secondButton?.dispatchEvent(new Event('click'));
    await fixture.whenStable();
    fixture.detectChanges();

    expect(followCalls).toEqual([`follow:${identityId}`, `unfollow:${identityId}`]);
    expect((fixture.nativeElement as HTMLElement).textContent).toContain('Follow state updated');
  });

  it('shows retryable section states when BFF facade calls fail', async () => {
    await configurePage({
      getProfile: () => throwError(() => new Error('BFF unavailable')),
      getFollowers: () => throwError(() => new Error('followers unavailable')),
      getFollowing: () => throwError(() => new Error('following unavailable')),
      getSavedItems: () => throwError(() => new Error('saved unavailable')),
    });

    const fixture = TestBed.createComponent(PublicProfilePage);
    await fixture.whenStable();
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;

    expect(compiled.textContent).toContain('Profile could not load');
    expect(compiled.textContent).toContain('Try again from Profiles');
  });
});

import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { BehaviorSubject, Observable, of, throwError } from 'rxjs';

import { AuthSessionState } from '../../core/auth/auth-session.models';
import { AuthSessionService } from '../../core/auth/auth-session.service';
import { UserProfileApi } from '../../core/user-profiles/infrastructure/user-profile.api';
import { UserSavedItemsResponse } from '../../core/user-profiles/user-profile.models';
import { SavedPage } from './saved.page';

describe('SavedPage', () => {
  const identityId = '3bbd95ab-b927-4ec1-bbf3-9a82e20ac2f5';
  const savedItems: UserSavedItemsResponse = {
    posts: [{ postId: 'post-1', savedAtUtc: '2026-04-25T00:00:00Z' }],
    dishes: [{ dishId: 'dish-1', savedAtUtc: '2026-04-26T00:00:00Z' }],
    places: [{ placeId: 'place-1', savedAtUtc: '2026-04-27T00:00:00Z' }],
  };
  const authState = new BehaviorSubject<AuthSessionState>({
    loadStatus: 'authenticated',
    session: {
      user: {
        subject: 'subject-1',
        preferredUsername: 'demo',
        email: 'demo@example.com',
        roles: ['user'],
      },
      identity: {
        identityId,
        state: 'Active',
        bootstrapPending: false,
        downstreamBootstrapPending: false,
      },
      serverTimeUtc: '2026-04-25T00:00:00Z',
    },
    isLoggingOut: false,
    lastLoadFailed: false,
  });

  const configurePage = async (options?: {
    getSavedItems?: () => Observable<UserSavedItemsResponse>;
  }) => {
    await TestBed.configureTestingModule({
      imports: [SavedPage],
      providers: [
        provideRouter([]),
        {
          provide: AuthSessionService,
          useValue: {
            authState: () => authState.value,
          },
        },
        {
          provide: UserProfileApi,
          useValue: {
            getSavedItems: () => (options?.getSavedItems ?? (() => of(savedItems)))(),
          },
        },
      ],
    }).compileComponents();
  };

  afterEach(() => {
    TestBed.resetTestingModule();
  });

  it('shows saved posts, dishes, and places as a dedicated revisit surface', async () => {
    await configurePage();

    const fixture = TestBed.createComponent(SavedPage);
    await fixture.whenStable();
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;

    expect(compiled.textContent).toContain('Saved content');
    expect(compiled.textContent).toContain('Saved posts');
    expect(compiled.textContent).toContain('Post post-1');
    expect(compiled.textContent).toContain('Dish dish-1');
    expect(compiled.textContent).toContain('Place place-1');
    expect(compiled.textContent).toContain('Find posts for this dish');
    expect(compiled.textContent).toContain('Find posts for this place');
  });

  it('shows explicit empty and error saved states', async () => {
    await configurePage({
      getSavedItems: () => of({ posts: [], dishes: [], places: [] }),
    });

    const emptyFixture = TestBed.createComponent(SavedPage);
    await emptyFixture.whenStable();
    emptyFixture.detectChanges();

    expect((emptyFixture.nativeElement as HTMLElement).textContent).toContain('No saved food yet');

    TestBed.resetTestingModule();
    await configurePage({
      getSavedItems: () => throwError(() => new Error('saved failed')),
    });

    const errorFixture = TestBed.createComponent(SavedPage);
    await errorFixture.whenStable();
    errorFixture.detectChanges();

    expect((errorFixture.nativeElement as HTMLElement).textContent).toContain('Saved content could not load');
  });
});

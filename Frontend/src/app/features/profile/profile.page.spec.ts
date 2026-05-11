import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { ActivatedRoute } from '@angular/router';
import { BehaviorSubject, Observable, of, throwError } from 'rxjs';

import { AuthSessionState } from '../../core/auth/auth-session.models';
import { AuthSessionService } from '../../core/auth/auth-session.service';
import { UserProfileApi } from '../../core/user-profiles/infrastructure/user-profile.api';
import { PostApi } from '../../core/posts/infrastructure/post.api';
import { PostSearchResponse } from '../../core/posts/post.models';
import {
  UpdateUserProfileRequest,
  UserProfileResponse,
  UserSavedItemsResponse,
} from '../../core/user-profiles/user-profile.models';
import { ProfilePage } from './profile.page';
import { ProfileStateService } from './profile-state.service';

describe('ProfilePage', () => {
  const identityId = '3bbd95ab-b927-4ec1-bbf3-9a82e20ac2f5';
  const profile: UserProfileResponse = {
    identityId,
    displayName: 'Demo Explorer',
    avatarUrl: null,
    bio: 'Always looking for bun bo hue.',
    preferredCuisines: ['Vietnamese', 'Street food'],
    dislikedIngredients: ['Cilantro'],
    dietaryRestrictions: ['No peanuts'],
    followerCount: 4,
    followingCount: 7,
  };
  const savedItems: UserSavedItemsResponse = {
    posts: [{ postId: 'post-1', savedAtUtc: '2026-04-25T00:00:00Z' }],
    dishes: [{ dishId: 'dish-1', savedAtUtc: '2026-04-25T00:00:00Z' }],
    places: [],
  };
  const authoredPosts: PostSearchResponse = {
    items: [
      {
        id: 'authored-post-1',
        authorId: identityId,
        dishId: 'dish-authored-1',
        placeId: null,
        caption: 'My best cơm tấm note.',
        rating: 5,
        priceRange: '$$',
        tags: ['com-tam'],
        primaryMediaUrl: null,
        isHidden: false,
        likeCount: 3,
        repostCount: 0,
        commentCount: 1,
        createdAtUtc: '2026-04-26T00:00:00Z',
      },
    ],
    limit: 12,
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
    getProfile?: () => Observable<UserProfileResponse>;
    updateProfile?: (request: UpdateUserProfileRequest) => Observable<UserProfileResponse>;
    getSavedItems?: () => Observable<UserSavedItemsResponse>;
    searchPosts?: () => Observable<PostSearchResponse>;
  }) => {
    const updateRequests: UpdateUserProfileRequest[] = [];

    await TestBed.configureTestingModule({
      imports: [ProfilePage],
      providers: [
        provideRouter([]),
        {
          provide: ActivatedRoute,
          useValue: {},
        },
        {
          provide: AuthSessionService,
          useValue: {
            authState: () => authState.value,
          },
        },
        {
          provide: UserProfileApi,
          useValue: {
            getProfile: () => (options?.getProfile ?? (() => of(profile)))(),
            updateProfile: (_identityId: string, request: UpdateUserProfileRequest) => {
              updateRequests.push(request);
              return (options?.updateProfile ?? (() => of({ ...profile, ...request })))(request);
            },
            getSavedItems: () => (options?.getSavedItems ?? (() => of(savedItems)))(),
          },
        },
        {
          provide: PostApi,
          useValue: {
            searchPosts: () => (options?.searchPosts ?? (() => of(authoredPosts)))(),
          },
        },
        ProfileStateService,
      ],
    }).compileComponents();

    return { updateRequests };
  };

  afterEach(() => {
    TestBed.resetTestingModule();
  });

  it('loads the authenticated profile form and saved item identifiers', async () => {
    await configurePage();

    const fixture = TestBed.createComponent(ProfilePage);
    await fixture.whenStable();
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;

    expect(compiled.textContent).toContain('Demo Explorer');
    expect(compiled.textContent).toContain('Post post-1');
    expect(compiled.textContent).toContain('Dish dish-1');
    expect(compiled.textContent).toContain('Authored posts');
    expect(compiled.textContent).toContain('My best cơm tấm note.');
    expect(compiled.textContent).toContain('No saved places.');
  });

  it('submits a normalized profile update request and shows success', async () => {
    const { updateRequests } = await configurePage();
    const fixture = TestBed.createComponent(ProfilePage);
    await fixture.whenStable();
    fixture.detectChanges();

    const component = fixture.componentInstance;
    const form = component['profileForm'];
    form.controls.displayName.setValue('  Updated Explorer  ');
    form.controls.avatarUrl.setValue('');
    form.controls.bio.setValue('  New bio  ');
    form.controls.preferredCuisines.setValue('Vietnamese, Thai, , Seafood');
    form.controls.dislikedIngredients.setValue('Cilantro');
    form.controls.dietaryRestrictions.setValue('No peanuts, Halal');

    const formElement = (fixture.nativeElement as HTMLElement).querySelector('form');
    formElement?.dispatchEvent(new Event('submit'));
    await fixture.whenStable();
    fixture.detectChanges();

    expect(updateRequests).toEqual([
      {
        displayName: 'Updated Explorer',
        avatarUrl: null,
        bio: 'New bio',
        preferredCuisines: ['Vietnamese', 'Thai', 'Seafood'],
        dislikedIngredients: ['Cilantro'],
        dietaryRestrictions: ['No peanuts', 'Halal'],
      },
    ]);
    expect((fixture.nativeElement as HTMLElement).textContent).toContain('Profile saved');
  });

  it('shows explicit profile and saved error states', async () => {
    await configurePage({
      getProfile: () => throwError(() => new Error('profile failed')),
      getSavedItems: () => throwError(() => new Error('saved failed')),
    });

    const fixture = TestBed.createComponent(ProfilePage);
    await fixture.whenStable();
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;

    expect(compiled.textContent).toContain('Your profile could not load');
    expect(compiled.textContent).toContain('Saved items could not load');
  });
});

import { signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { of } from 'rxjs';
import { vi } from 'vitest';

import { AuthSessionState } from '../../core/auth/auth-session.models';
import { AuthSessionService } from '../../core/auth/auth-session.service';
import { BROWSER_WINDOW } from '../../core/auth/browser-window.token';
import { DishApi } from '../../core/dishes/infrastructure/dish.api';
import { PlaceApi } from '../../core/places/infrastructure/place.api';
import { PostApi } from '../../core/posts/infrastructure/post.api';
import { PostSearchRequest, PostSearchResponse } from '../../core/posts/post.models';
import { UserProfileApi } from '../../core/user-profiles/infrastructure/user-profile.api';
import { FollowingPage } from './following.page';

describe('FollowingPage', () => {
  const identityId = '11111111-1111-1111-1111-111111111111';
  const followedIdentityId = '22222222-2222-2222-2222-222222222222';
  const authenticatedState: AuthSessionState = {
    loadStatus: 'authenticated',
    session: {
      user: {
        subject: 'subject-123',
        preferredUsername: 'demo-user',
        email: 'demo@example.com',
        roles: ['user'],
      },
      identity: {
        identityId,
        state: 'Active',
        bootstrapPending: false,
        downstreamBootstrapPending: false,
      },
      serverTimeUtc: '2026-05-04T00:00:00Z',
    },
    isLoggingOut: false,
    lastLoadFailed: false,
  };
  const postSearchResponse: PostSearchResponse = {
    items: [
      {
        id: '33333333-3333-3333-3333-333333333333',
        authorId: followedIdentityId,
        dishId: '44444444-4444-4444-4444-444444444444',
        placeId: null,
        caption: 'Followed author posted bún bò Huế.',
        rating: 5,
        priceRange: '$$',
        tags: ['bun-bo-hue'],
        primaryMediaUrl: 'https://example.com/bun-bo-hue.jpg',
        isHidden: false,
        likeCount: 4,
        repostCount: 0,
        commentCount: 2,
        createdAtUtc: '2026-05-04T00:00:00Z',
      },
    ],
    limit: 12,
  };
  const authState = signal(authenticatedState);
  const clipboard = { writeText: (_value: string) => Promise.resolve() };
  let getFollowingFeedMock: ReturnType<typeof vi.fn>;

  beforeEach(async () => {
    getFollowingFeedMock = vi.fn((_limit: number) => of(postSearchResponse));

    await TestBed.configureTestingModule({
      imports: [FollowingPage],
      providers: [
        provideRouter([]),
        {
          provide: AuthSessionService,
          useValue: { authState },
        },
        {
          provide: UserProfileApi,
          useValue: {
            getFollowing: () => {
              throw new Error('Following page should use the BFF following feed instead of client fan-out.');
            },
            savePost: (_identityId: string, postId: string) => of({ userIdentityId: identityId, targetId: postId, targetType: 'Post', saved: true }),
          },
        },
        {
          provide: PostApi,
          useValue: {
            getFollowingFeed: getFollowingFeedMock,
            searchPosts: (_request: PostSearchRequest) => of(postSearchResponse),
            likePost: (postId: string) => of({ postId, actorId: identityId, liked: true, likeCount: 5 }),
          },
        },
        {
          provide: DishApi,
          useValue: {
            getDish: (dishId: string) => of({
              id: dishId,
              name: 'Bún bò Huế',
              normalizedName: 'bun bo hue',
              description: null,
              category: 'Noodles',
              cuisineType: 'Vietnamese',
              aliases: [],
              ingredients: [],
              dietaryFlags: [],
              status: 'Active',
              createdAtUtc: '2026-05-04T00:00:00Z',
              updatedAtUtc: '2026-05-04T00:00:00Z',
              deprecatedAtUtc: null,
            }),
          },
        },
        {
          provide: PlaceApi,
          useValue: {
            getPlace: () => {
              throw new Error('Following page fixture does not include place labels.');
            },
          },
        },
        {
          provide: BROWSER_WINDOW,
          useValue: {
            location: { origin: 'https://foodsocial.test' },
            navigator: { clipboard },
          },
        },
      ],
    }).compileComponents();
  });

  afterEach(() => {
    TestBed.resetTestingModule();
  });

  it('loads followed-author posts from the follow graph and Post search', async () => {
    const fixture = TestBed.createComponent(FollowingPage);

    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();

    const text = (fixture.nativeElement as HTMLElement).textContent ?? '';

    expect(getFollowingFeedMock).toHaveBeenCalledWith(24);
    expect(text).toContain('Follow graph first');
    expect(text).toContain('Followed author posted bún bò Huế.');
    expect(text).toContain('4 likes');
  });
});

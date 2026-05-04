import { signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { of } from 'rxjs';
import { vi } from 'vitest';

import { AuthSessionState } from '../../core/auth/auth-session.models';
import { AuthSessionService } from '../../core/auth/auth-session.service';
import { BROWSER_WINDOW } from '../../core/auth/browser-window.token';
import { DishSearchRequest, DishSearchResponse } from '../../core/dishes/dish.models';
import { DishApi } from '../../core/dishes/infrastructure/dish.api';
import { PlaceSearchRequest, PlaceSearchResponse } from '../../core/places/place.models';
import { PlaceApi } from '../../core/places/infrastructure/place.api';
import { PostSearchRequest, PostSearchResponse } from '../../core/posts/post.models';
import { PostApi } from '../../core/posts/infrastructure/post.api';
import { SearchAssistApi } from '../../core/search/infrastructure/search-assist.api';
import { UserProfileApi } from '../../core/user-profiles/infrastructure/user-profile.api';
import { SearchPage } from './search.page';

describe('SearchPage', () => {
  const identityId = '11111111-1111-1111-1111-111111111111';
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
  const dishSearchResponse: DishSearchResponse = {
    items: [
      {
        id: '22222222-2222-2222-2222-222222222222',
        name: 'Phở bò',
        category: 'Noodle soup',
        cuisineType: 'Vietnamese',
        aliases: ['pho bo'],
        status: 'Active',
      },
    ],
    limit: 8,
  };
  const placeSearchResponse: PlaceSearchResponse = {
    items: [
      {
        id: '33333333-3333-3333-3333-333333333333',
        name: 'Quán Ăn Ngon',
        addressText: '18 Phan Bội Châu',
        area: 'District 1',
        city: 'Ho Chi Minh City',
        country: 'Vietnam',
        cuisineTypes: ['Vietnamese'],
        priceRange: '$$',
        status: 'Active',
      },
    ],
    limit: 8,
  };
  const postSearchResponse: PostSearchResponse = {
    items: [
      {
        id: '44444444-4444-4444-4444-444444444444',
        authorId: identityId,
        dishId: dishSearchResponse.items[0].id,
        placeId: placeSearchResponse.items[0].id,
        caption: 'Search found rich phở broth.',
        rating: 5,
        priceRange: '$$',
        tags: ['pho'],
        primaryMediaUrl: 'https://example.com/pho.jpg',
        isHidden: false,
        likeCount: 2,
        repostCount: 0,
        commentCount: 1,
        createdAtUtc: '2026-05-04T00:00:00Z',
      },
    ],
    limit: 12,
  };

  let dishApiMock: { searchDishes: ReturnType<typeof vi.fn> };
  let placeApiMock: { searchPlaces: ReturnType<typeof vi.fn> };
  let postApiMock: { searchPosts: ReturnType<typeof vi.fn>; likePost: ReturnType<typeof vi.fn> };
  let userProfileApiMock: { savePost: ReturnType<typeof vi.fn>; saveDish: ReturnType<typeof vi.fn>; savePlace: ReturnType<typeof vi.fn> };
  let searchAssistApiMock: { assist: ReturnType<typeof vi.fn> };

  beforeEach(async () => {
    dishApiMock = {
      searchDishes: vi.fn((_request: DishSearchRequest) => of(dishSearchResponse)),
    };
    placeApiMock = {
      searchPlaces: vi.fn((_request: PlaceSearchRequest) => of(placeSearchResponse)),
    };
    postApiMock = {
      searchPosts: vi.fn((_request: PostSearchRequest) => of(postSearchResponse)),
      likePost: vi.fn((postId: string) => of({ postId, actorId: identityId, liked: true, likeCount: 3 })),
    };
    searchAssistApiMock = {
      assist: vi.fn((_request: { query: string; location: string | null }) => of({
        normalizedQuery: 'phở',
        assistStatus: 'ai-disabled-fallback',
        fallbackUsed: true,
        suggestions: [{ type: 'dish', value: 'phở', reason: 'Search dishes.' }],
        filters: {},
        explanation: 'Deterministic fallback.',
      })),
    };
    userProfileApiMock = {
      savePost: vi.fn((_profileId: string, postId: string) => of({ userIdentityId: identityId, targetId: postId, targetType: 'Post', saved: true })),
      saveDish: vi.fn((_profileId: string, dishId: string) => of({ userIdentityId: identityId, targetId: dishId, targetType: 'Dish', saved: true })),
      savePlace: vi.fn((_profileId: string, placeId: string) => of({ userIdentityId: identityId, targetId: placeId, targetType: 'Place', saved: true })),
    };

    await TestBed.configureTestingModule({
      imports: [SearchPage],
      providers: [
        provideRouter([]),
        {
          provide: AuthSessionService,
          useValue: {
            authState: signal(authenticatedState),
          },
        },
        { provide: DishApi, useValue: dishApiMock },
        { provide: PlaceApi, useValue: placeApiMock },
        { provide: PostApi, useValue: postApiMock },
        { provide: UserProfileApi, useValue: userProfileApiMock },
        { provide: SearchAssistApi, useValue: searchAssistApiMock },
        {
          provide: BROWSER_WINDOW,
          useValue: {
            location: { origin: 'https://foodsocial.test' },
            navigator: { clipboard: { writeText: vi.fn() } },
          },
        },
      ],
    }).compileComponents();
  });

  afterEach(() => {
    TestBed.resetTestingModule();
  });

  it('searches dishes, places, and posts through deterministic BFF APIs', async () => {
    const fixture = TestBed.createComponent(SearchPage);
    fixture.detectChanges();

    const element = fixture.nativeElement as HTMLElement;
    const component = fixture.componentInstance as unknown as {
      queryControl: { setValue(value: string): void };
      runSearch(): void;
    };

    component.queryControl.setValue('phở');
    component.runSearch();
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();

    expect(dishApiMock.searchDishes).toHaveBeenCalledWith({ query: 'phở', limit: 8 });
    expect(placeApiMock.searchPlaces).toHaveBeenCalledWith({ query: 'phở', limit: 8 });
    expect(postApiMock.searchPosts).toHaveBeenCalledWith({ query: 'phở', limit: 12 });
    expect(searchAssistApiMock.assist).toHaveBeenCalledWith({ query: 'phở', location: null });
    expect(element.textContent).toContain('Phở bò');
    expect(element.textContent).toContain('Quán Ăn Ngon');
    expect(element.textContent).toContain('Search found rich phở broth.');
    expect(element.textContent).toContain('ai-disabled-fallback');
  });
});

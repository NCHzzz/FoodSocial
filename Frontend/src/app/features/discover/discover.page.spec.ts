import { signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { By } from '@angular/platform-browser';
import { provideRouter } from '@angular/router';
import { of } from 'rxjs';

import { AuthSessionState } from '../../core/auth/auth-session.models';
import { AuthSessionService } from '../../core/auth/auth-session.service';
import { DishApi } from '../../core/dishes/infrastructure/dish.api';
import { DishSearchRequest, DishSearchResponse } from '../../core/dishes/dish.models';
import { PlaceApi } from '../../core/places/infrastructure/place.api';
import { PostApi } from '../../core/posts/infrastructure/post.api';
import { DiscoverFeedResponse } from '../../core/posts/post.models';
import { UserProfileApi } from '../../core/user-profiles/infrastructure/user-profile.api';
import { PlaceSearchRequest, PlaceSearchResponse } from '../../core/places/place.models';
import { DishSearchSelectComponent } from '../dishes/dish-search-select.component';
import { PlaceSearchSelectComponent } from '../places/place-search-select.component';
import { DiscoverPage } from './discover.page';

describe('DiscoverPage', () => {
  const guestState: AuthSessionState = {
    loadStatus: 'guest',
    session: null,
    isLoggingOut: false,
    lastLoadFailed: false,
  };

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
        identityId: '33333333-3333-3333-3333-333333333333',
        state: 'Active',
        bootstrapPending: false,
        downstreamBootstrapPending: false,
      },
      serverTimeUtc: '2026-04-28T00:00:00Z',
    },
    isLoggingOut: false,
    lastLoadFailed: false,
  };

  const dishSearchResponse: DishSearchResponse = {
    items: [
      {
        id: '11111111-1111-1111-1111-111111111111',
        name: 'Phở bò',
        category: 'Noodle soup',
        cuisineType: 'Vietnamese',
        aliases: ['pho bo'],
        status: 'Active',
      },
    ],
    limit: 10,
  };

  const discoverFeedResponse: DiscoverFeedResponse = {
    items: [
      {
        id: '44444444-4444-4444-4444-444444444444',
        authorId: '33333333-3333-3333-3333-333333333333',
        dishId: '11111111-1111-1111-1111-111111111111',
        placeId: '22222222-2222-2222-2222-222222222222',
        caption: 'Real phở broth from the live feed.',
        rating: 5,
        priceRange: '$$',
        tags: ['pho'],
        primaryMediaUrl: 'https://example.com/pho.jpg',
        isHidden: false,
        likeCount: 2,
        commentCount: 1,
        createdAtUtc: '2026-05-04T00:00:00Z',
      },
    ],
    limit: 12,
  };

  const placeSearchResponse: PlaceSearchResponse = {
    items: [
      {
        id: '22222222-2222-2222-2222-222222222222',
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
    limit: 10,
  };

  let authState = signal(guestState);

  beforeEach(async () => {
    authState = signal(guestState);

    await TestBed.configureTestingModule({
      imports: [DiscoverPage],
      providers: [
        provideRouter([]),
        {
          provide: AuthSessionService,
          useValue: {
            authState,
          },
        },
        {
          provide: DishApi,
          useValue: {
            searchDishes: (_request: DishSearchRequest) => of(dishSearchResponse),
          },
        },
        {
          provide: PlaceApi,
          useValue: {
            searchPlaces: (_request: PlaceSearchRequest) => of(placeSearchResponse),
          },
        },
        {
          provide: PostApi,
          useValue: {
            getDiscoverFeed: (_limit: number) => of(discoverFeedResponse),
            likePost: (_postId: string) => of({ postId: discoverFeedResponse.items[0].id, actorId: authenticatedState.session!.identity.identityId, liked: true, likeCount: 3 }),
          },
        },
        {
          provide: UserProfileApi,
          useValue: {
            savePost: (_identityId: string, _postId: string) => of({ userIdentityId: authenticatedState.session!.identity.identityId, targetId: discoverFeedResponse.items[0].id, targetType: 'Post', saved: true }),
          },
        },
      ],
    }).compileComponents();
  });

  afterEach(() => {
    TestBed.resetTestingModule();
  });

  it('renders real catalog search surfaces and selected context for guests', async () => {
    const fixture = TestBed.createComponent(DiscoverPage);

    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;

    expect(compiled.textContent).toContain('Catalog context');
    expect(compiled.textContent).toContain('Live Discover feed');
    expect(compiled.textContent).toContain('Real phở broth from the live feed.');
    expect(compiled.textContent).toContain('Phở bò');
    expect(compiled.textContent).toContain('Quán Ăn Ngon');
    expect(compiled.textContent).toContain('Select a dish or place above');

    const dishSelector = fixture.debugElement.query(By.directive(DishSearchSelectComponent)).componentInstance as DishSearchSelectComponent;
    const placeSelector = fixture.debugElement.query(By.directive(PlaceSearchSelectComponent)).componentInstance as PlaceSearchSelectComponent;
    dishSelector.dishSelected.emit(dishSearchResponse.items[0]);
    placeSelector.placeSelected.emit(placeSearchResponse.items[0]);
    fixture.detectChanges();

    expect(compiled.textContent).toContain('Dish selected');
    expect(compiled.textContent).toContain('Place selected');
    expect(compiled.textContent).toContain('District 1');
    expect(compiled.textContent).not.toContain('Select a dish or place above');
  });

  it('renders authenticated session state and role context', async () => {
    authState.set(authenticatedState);
    const fixture = TestBed.createComponent(DiscoverPage);

    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();

    const text = (fixture.nativeElement as HTMLElement).textContent ?? '';

    expect(text).toContain('Signed in');
    expect(text).toContain('Active');
    expect(text).toContain('user');
  });
});

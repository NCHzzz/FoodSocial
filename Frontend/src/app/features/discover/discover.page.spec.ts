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

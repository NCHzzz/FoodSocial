import { TestBed } from '@angular/core/testing';
import { By } from '@angular/platform-browser';
import { provideRouter } from '@angular/router';
import { of } from 'rxjs';

import { DishApi } from '../../core/dishes/infrastructure/dish.api';
import { DishSearchRequest, DishSearchResponse } from '../../core/dishes/dish.models';
import { PlaceApi } from '../../core/places/infrastructure/place.api';
import { PlaceSearchRequest, PlaceSearchResponse } from '../../core/places/place.models';
import { DishSearchSelectComponent } from '../dishes/dish-search-select.component';
import { PlaceSearchSelectComponent } from '../places/place-search-select.component';
import { PostCreatePage } from './post-create.page';

describe('PostCreatePage', () => {
  const dishSearchResponse: DishSearchResponse = {
    items: [
      {
        id: '11111111-1111-1111-1111-111111111111',
        name: 'Bún chả',
        category: 'Grilled pork noodles',
        cuisineType: 'Vietnamese',
        aliases: [],
        status: 'Active',
      },
    ],
    limit: 10,
  };

  const placeSearchResponse: PlaceSearchResponse = {
    items: [
      {
        id: '22222222-2222-2222-2222-222222222222',
        name: 'Bún Chả Hương Liên',
        addressText: '24 Lê Văn Hưu',
        area: 'Hai Bà Trưng',
        city: 'Hanoi',
        country: 'Vietnam',
        cuisineTypes: ['Vietnamese'],
        priceRange: '$$',
        status: 'Active',
      },
    ],
    limit: 10,
  };

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [PostCreatePage],
      providers: [
        provideRouter([]),
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

  it('uses real catalog selectors while keeping publish locked', async () => {
    const fixture = TestBed.createComponent(PostCreatePage);

    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;

    expect(compiled.textContent).toContain('Publishing is locked; catalog exploration is real');
    expect(compiled.textContent).toContain('Bún chả');
    expect(compiled.textContent).toContain('Bún Chả Hương Liên');
    expect(compiled.textContent).toContain('Publish remains disabled until media upload and publish orchestration are enabled');
    expect(Array.from(compiled.querySelectorAll('button')).some((button) => button.textContent?.includes('Publish'))).toBe(false);

    const dishSelector = fixture.debugElement.query(By.directive(DishSearchSelectComponent)).componentInstance as DishSearchSelectComponent;
    const placeSelector = fixture.debugElement.query(By.directive(PlaceSearchSelectComponent)).componentInstance as PlaceSearchSelectComponent;
    dishSelector.dishSelected.emit(dishSearchResponse.items[0]);
    placeSelector.placeSelected.emit(placeSearchResponse.items[0]);
    fixture.detectChanges();

    expect(compiled.textContent).toContain('Dish identity');
    expect(compiled.textContent).toContain('Place context');
    expect(compiled.textContent).toContain('Hai Bà Trưng');
  });
});

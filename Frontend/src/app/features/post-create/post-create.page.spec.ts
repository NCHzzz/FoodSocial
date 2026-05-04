import { TestBed } from '@angular/core/testing';
import { By } from '@angular/platform-browser';
import { provideRouter } from '@angular/router';
import { of } from 'rxjs';

import { DishApi } from '../../core/dishes/infrastructure/dish.api';
import { DishSearchRequest, DishSearchResponse } from '../../core/dishes/dish.models';
import { PlaceApi } from '../../core/places/infrastructure/place.api';
import { PostApi } from '../../core/posts/infrastructure/post.api';
import { CreatePostRequest } from '../../core/posts/post.models';
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
        {
          provide: PostApi,
          useValue: {
            createPost: (request: CreatePostRequest) => of({
              id: '33333333-3333-3333-3333-333333333333',
              authorId: request.authorId,
              dishId: request.dishId,
              placeId: request.placeId ?? null,
              caption: request.caption,
              rating: request.rating ?? null,
              priceRange: request.priceRange ?? null,
              tags: request.tags ?? [],
              locationText: request.locationText ?? null,
              status: 'Published',
              isHidden: false,
              likeCount: 0,
              commentCount: 0,
              media: [{ id: '44444444-4444-4444-4444-444444444444', url: request.media[0].url, type: 'image', thumbnailUrl: null, caption: null, isPrimary: true, sortOrder: 0 }],
              comments: [],
              createdAtUtc: '2026-05-04T00:00:00Z',
              updatedAtUtc: '2026-05-04T00:00:00Z',
            }),
          },
        },
      ],
    }).compileComponents();
  });

  afterEach(() => {
    TestBed.resetTestingModule();
  });

  it('uses real catalog selectors and publishes a URL-backed post', async () => {
    const fixture = TestBed.createComponent(PostCreatePage);

    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;

    expect(compiled.textContent).toContain('Canonical dish first, place optional');
    expect(compiled.textContent).toContain('Bún chả');
    expect(compiled.textContent).toContain('Bún Chả Hương Liên');
    expect(compiled.textContent).toContain('Publish post');

    const dishSelector = fixture.debugElement.query(By.directive(DishSearchSelectComponent)).componentInstance as DishSearchSelectComponent;
    const placeSelector = fixture.debugElement.query(By.directive(PlaceSearchSelectComponent)).componentInstance as PlaceSearchSelectComponent;
    dishSelector.dishSelected.emit(dishSearchResponse.items[0]);
    placeSelector.placeSelected.emit(placeSearchResponse.items[0]);
    fixture.detectChanges();

    expect(compiled.textContent).toContain('Selected dish');
    expect(compiled.textContent).toContain('Selected place');
    expect(compiled.textContent).toContain('Hai Bà Trưng');

    fixture.componentInstance['postForm'].controls.primaryMediaUrl.setValue('https://example.com/bun-cha.jpg');
    fixture.componentInstance['postForm'].controls.caption.setValue('Smoky pork, bright herbs, balanced sauce.');
    expect(fixture.componentInstance['postForm'].valid).toBe(true);
    expect(fixture.componentInstance['selectedDish']()).toEqual(dishSearchResponse.items[0]);
    expect(fixture.componentInstance['canPublish']()).toBe(true);
    fixture.detectChanges();

    fixture.componentInstance['publishPost']();
    await fixture.whenStable();
    fixture.detectChanges();

    expect(compiled.textContent).toContain('Post published');
    expect(compiled.textContent).toContain('Open it now');
  });
});

import { TestBed } from '@angular/core/testing';
import { By } from '@angular/platform-browser';
import { provideRouter } from '@angular/router';
import { of } from 'rxjs';
import { vi } from 'vitest';

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
  let createPostMock: ReturnType<typeof vi.fn>;
  let uploadMediaMock: ReturnType<typeof vi.fn>;

  beforeEach(async () => {
    createPostMock = vi.fn((request: CreatePostRequest) => of({
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
      repostCount: 0,
      commentCount: 0,
      media: [{ id: '44444444-4444-4444-4444-444444444444', url: request.media[0].url, type: 'image', thumbnailUrl: request.media[0].thumbnailUrl, caption: null, isPrimary: true, sortOrder: 0 }],
      comments: [],
      createdAtUtc: '2026-05-04T00:00:00Z',
      updatedAtUtc: '2026-05-04T00:00:00Z',
    }));
    uploadMediaMock = vi.fn((_file: File) => of({
      uploadId: '55555555-5555-5555-5555-555555555555',
      url: '/api/media/uploads/55555555-5555-5555-5555-555555555555/original',
      thumbnailUrl: '/api/media/uploads/55555555-5555-5555-5555-555555555555/thumbnail',
      contentType: 'image/jpeg',
      sizeBytes: 8,
      cleanupDueAtUtc: '2026-05-11T00:00:00Z',
      status: 'StoredPendingAttachmentWithDerivativePlaceholder',
    }));

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
            createPost: createPostMock,
            uploadMedia: uploadMediaMock,
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

  it('attaches uploaded thumbnail URL when publishing uploaded media', async () => {
    const fixture = TestBed.createComponent(PostCreatePage);
    fixture.detectChanges();
    await fixture.whenStable();

    const dishSelector = fixture.debugElement.query(By.directive(DishSearchSelectComponent)).componentInstance as DishSearchSelectComponent;
    dishSelector.dishSelected.emit(dishSearchResponse.items[0]);
    fixture.componentInstance['postForm'].controls.caption.setValue('Uploaded food image with thumbnail.');

    const file = new File([new Uint8Array([0xFF, 0xD8, 0xFF, 0xE0])], 'bun-cha.jpg', { type: 'image/jpeg' });
    const input = document.createElement('input');
    Object.defineProperty(input, 'files', { value: { item: () => file } });
    fixture.componentInstance['uploadMedia']({ target: input } as unknown as Event);
    await fixture.whenStable();
    fixture.componentInstance['publishPost']();
    await fixture.whenStable();

    const request = createPostMock.mock.calls[0][0] as CreatePostRequest;
    expect(request.media[0].url).toBe('/api/media/uploads/55555555-5555-5555-5555-555555555555/original');
    expect(request.media[0].thumbnailUrl).toBe('/api/media/uploads/55555555-5555-5555-5555-555555555555/thumbnail');
  });
});

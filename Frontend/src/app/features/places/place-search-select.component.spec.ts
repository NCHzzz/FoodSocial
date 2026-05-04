import { TestBed } from '@angular/core/testing';
import { Observable, Subject, of, throwError } from 'rxjs';

import { PlaceApi } from '../../core/places/infrastructure/place.api';
import { PlaceSearchResponse } from '../../core/places/place.models';
import { PlaceSearchSelectComponent } from './place-search-select.component';

describe('PlaceSearchSelectComponent', () => {
  const placeSearchResponse: PlaceSearchResponse = {
    items: [
      {
        id: '11111111-1111-1111-1111-111111111111',
        name: 'Quán Ăn Ngon',
        addressText: '18 Phan Bội Châu',
        area: 'District 1',
        city: 'Ho Chi Minh City',
        country: 'Vietnam',
        cuisineTypes: ['vietnamese'],
        priceRange: '$$',
        status: 'Active',
      },
    ],
    limit: 10,
  };

  const configureComponent = async (options?: {
    searchPlaces?: () => Observable<PlaceSearchResponse>;
  }) => {
    const queries: string[] = [];
    const selectedPlaceIds: string[] = [];

    await TestBed.configureTestingModule({
      imports: [PlaceSearchSelectComponent],
      providers: [
        {
          provide: PlaceApi,
          useValue: {
            searchPlaces: (request: { query: string | null }) => {
              queries.push(request.query ?? '');
              return (options?.searchPlaces ?? (() => of(placeSearchResponse)))();
            },
          },
        },
      ],
    }).compileComponents();

    const fixture = TestBed.createComponent(PlaceSearchSelectComponent);
    fixture.componentRef.setInput('initialQuery', 'quan');
    fixture.componentInstance.placeSelected.subscribe((place) => selectedPlaceIds.push(place.id));

    return { fixture, queries, selectedPlaceIds };
  };

  afterEach(() => {
    TestBed.resetTestingModule();
  });

  it('searches places and emits the selected place', async () => {
    const { fixture, queries, selectedPlaceIds } = await configureComponent();

    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.textContent).toContain('Quán Ăn Ngon');
    expect(compiled.textContent).toContain('District 1');
    expect(compiled.textContent).toContain('Ho Chi Minh City');
    expect(queries).toEqual(['quan']);

    const button = Array.from(compiled.querySelectorAll('button'))
      .find((candidate) => candidate.textContent?.includes('Select'));
    button?.dispatchEvent(new Event('click'));
    fixture.detectChanges();

    expect(selectedPlaceIds).toEqual(['11111111-1111-1111-1111-111111111111']);
    expect(compiled.textContent).toContain('Selected place');
  });

  it('shows an explicit empty state when no places match', async () => {
    const { fixture } = await configureComponent({
      searchPlaces: () => of({ items: [], limit: 10 }),
    });

    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();

    expect((fixture.nativeElement as HTMLElement).textContent).toContain('No places found');
  });

  it('shows an explicit retryable error state when search fails', async () => {
    const { fixture } = await configureComponent({
      searchPlaces: () => throwError(() => new Error('BFF unavailable')),
    });

    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();

    expect((fixture.nativeElement as HTMLElement).textContent).toContain('Place search could not load');
  });

  it('keeps newer search results when an older request finishes later', async () => {
    const olderResponse = new Subject<PlaceSearchResponse>();
    const newerResponse = new Subject<PlaceSearchResponse>();
    let callCount = 0;
    const { fixture } = await configureComponent({
      searchPlaces: () => {
        callCount += 1;
        return callCount === 1 ? olderResponse.asObservable() : newerResponse.asObservable();
      },
    });

    fixture.detectChanges();
    await fixture.whenStable();

    const input = (fixture.nativeElement as HTMLElement).querySelector('input') as HTMLInputElement;
    input.value = 'bun cha';
    input.dispatchEvent(new Event('input'));
    fixture.detectChanges();

    const form = (fixture.nativeElement as HTMLElement).querySelector('form') as HTMLFormElement;
    form.dispatchEvent(new Event('submit'));
    fixture.detectChanges();
    await fixture.whenStable();

    newerResponse.next({
      items: [
        {
          id: '22222222-2222-2222-2222-222222222222',
          name: 'Bún Chả Hương Liên',
          addressText: '24 Lê Văn Hưu',
          area: 'Hai Bà Trưng',
          city: 'Hanoi',
          country: 'Vietnam',
          cuisineTypes: ['vietnamese'],
          priceRange: '$$',
          status: 'Active',
        },
      ],
      limit: 10,
    });
    newerResponse.complete();
    fixture.detectChanges();

    olderResponse.next(placeSearchResponse);
    olderResponse.complete();
    fixture.detectChanges();

    const text = (fixture.nativeElement as HTMLElement).textContent ?? '';
    expect(text).toContain('Bún Chả Hương Liên');
    expect(text).not.toContain('Quán Ăn Ngon');
  });
});

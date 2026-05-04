import { TestBed } from '@angular/core/testing';
import { Observable, Subject, of, throwError } from 'rxjs';

import { DishApi } from '../../core/dishes/infrastructure/dish.api';
import { DishSearchResponse } from '../../core/dishes/dish.models';
import { DishSearchSelectComponent } from './dish-search-select.component';

describe('DishSearchSelectComponent', () => {
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

  const configureComponent = async (options?: {
    searchDishes?: () => Observable<DishSearchResponse>;
  }) => {
    const queries: string[] = [];
    const selectedDishIds: string[] = [];

    await TestBed.configureTestingModule({
      imports: [DishSearchSelectComponent],
      providers: [
        {
          provide: DishApi,
          useValue: {
            searchDishes: (request: { query: string | null }) => {
              queries.push(request.query ?? '');
              return (options?.searchDishes ?? (() => of(dishSearchResponse)))();
            },
          },
        },
      ],
    }).compileComponents();

    const fixture = TestBed.createComponent(DishSearchSelectComponent);
    fixture.componentRef.setInput('initialQuery', 'pho');
    fixture.componentInstance.dishSelected.subscribe((dish) => selectedDishIds.push(dish.id));

    return { fixture, queries, selectedDishIds };
  };

  afterEach(() => {
    TestBed.resetTestingModule();
  });

  it('searches dishes and emits the selected dish', async () => {
    const { fixture, queries, selectedDishIds } = await configureComponent();

    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.textContent).toContain('Phở bò');
    expect(compiled.textContent).toContain('Noodle soup');
    expect(queries).toEqual(['pho']);

    const button = Array.from(compiled.querySelectorAll('button'))
      .find((candidate) => candidate.textContent?.includes('Select'));
    button?.dispatchEvent(new Event('click'));
    fixture.detectChanges();

    expect(selectedDishIds).toEqual(['11111111-1111-1111-1111-111111111111']);
    expect(compiled.textContent).toContain('Selected dish');
  });

  it('shows an explicit empty state when no dishes match', async () => {
    const { fixture } = await configureComponent({
      searchDishes: () => of({ items: [], limit: 10 }),
    });

    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();

    expect((fixture.nativeElement as HTMLElement).textContent).toContain('No dishes found');
  });

  it('shows an explicit retryable error state when search fails', async () => {
    const { fixture } = await configureComponent({
      searchDishes: () => throwError(() => new Error('BFF unavailable')),
    });

    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();

    expect((fixture.nativeElement as HTMLElement).textContent).toContain('Dish search could not load');
  });

  it('keeps newer search results when an older request finishes later', async () => {
    const olderResponse = new Subject<DishSearchResponse>();
    const newerResponse = new Subject<DishSearchResponse>();
    let callCount = 0;
    const { fixture } = await configureComponent({
      searchDishes: () => {
        callCount += 1;
        return callCount === 1 ? olderResponse.asObservable() : newerResponse.asObservable();
      },
    });

    fixture.detectChanges();
    await fixture.whenStable();

    const input = (fixture.nativeElement as HTMLElement).querySelector('input') as HTMLInputElement;
    input.value = 'bun';
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
          name: 'Bún chả',
          category: 'Noodles',
          cuisineType: 'Vietnamese',
          aliases: [],
          status: 'Active',
        },
      ],
      limit: 10,
    });
    newerResponse.complete();
    fixture.detectChanges();

    olderResponse.next(dishSearchResponse);
    olderResponse.complete();
    fixture.detectChanges();

    const text = (fixture.nativeElement as HTMLElement).textContent ?? '';
    expect(text).toContain('Bún chả');
    expect(text).not.toContain('Phở bò');
  });
});

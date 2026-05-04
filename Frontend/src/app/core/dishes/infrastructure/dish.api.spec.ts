import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';

import { DishSearchResponse } from '../dish.models';
import { DishApi } from './dish.api';

describe('DishApi', () => {
  const searchResponse: DishSearchResponse = {
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

  let httpTestingController: HttpTestingController;
  let api: DishApi;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
      ],
    });

    httpTestingController = TestBed.inject(HttpTestingController);
    api = TestBed.inject(DishApi);
  });

  afterEach(() => {
    httpTestingController.verify();
  });

  it('searches dishes through the BFF dish facade with browser credentials', () => {
    api.searchDishes({ query: 'pho', category: 'Noodle soup', cuisineType: 'Vietnamese', limit: 10 }).subscribe((response) => {
      expect(response).toEqual(searchResponse);
    });

    const request = httpTestingController.expectOne('/api/dishes/search?query=pho&category=Noodle%20soup&cuisineType=Vietnamese&limit=10');
    expect(request.request.method).toBe('GET');
    expect(request.request.withCredentials).toBe(true);

    request.flush(searchResponse);
  });

  it('omits blank search parameters and includes explicit lifecycle flags', () => {
    api.searchDishes({ query: '   ', category: '', cuisineType: null, includeDeprecated: true, limit: null }).subscribe((response) => {
      expect(response).toEqual(searchResponse);
    });

    const request = httpTestingController.expectOne('/api/dishes/search?includeDeprecated=true');
    expect(request.request.method).toBe('GET');
    expect(request.request.withCredentials).toBe(true);

    request.flush(searchResponse);
  });

  it('trims non-blank search parameters before sending them', () => {
    api.searchDishes({ query: '  pho  ', category: '  soup  ', cuisineType: '  Vietnamese  ', limit: 5 }).subscribe((response) => {
      expect(response).toEqual(searchResponse);
    });

    const request = httpTestingController.expectOne('/api/dishes/search?query=pho&category=soup&cuisineType=Vietnamese&limit=5');
    expect(request.request.method).toBe('GET');

    request.flush(searchResponse);
  });

  it('loads a dish detail through the BFF dish facade', () => {
    const dishId = '22222222-2222-2222-2222-222222222222';

    api.getDish(dishId).subscribe((dish) => {
      expect(dish.id).toBe(dishId);
      expect(dish.name).toBe('Bún chả');
    });

    const request = httpTestingController.expectOne(`/api/dishes/${dishId}`);
    expect(request.request.method).toBe('GET');
    expect(request.request.withCredentials).toBe(true);

    request.flush({
      id: dishId,
      name: 'Bún chả',
      normalizedName: 'bun cha',
      description: 'Grilled pork with noodles',
      category: 'Noodles',
      cuisineType: 'Vietnamese',
      aliases: ['bun cha ha noi'],
      ingredients: ['pork', 'rice noodles'],
      dietaryFlags: [],
      status: 'Active',
      createdAtUtc: '2026-04-27T00:00:00Z',
      updatedAtUtc: '2026-04-27T00:00:00Z',
      deprecatedAtUtc: null,
    });
  });
});

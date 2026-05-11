import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { credentialsInterceptor } from '../../http/credentials.interceptor';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';

import { PlaceSearchResponse } from '../place.models';
import { PlaceApi } from './place.api';

describe('PlaceApi', () => {
  const searchResponse: PlaceSearchResponse = {
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

  let httpTestingController: HttpTestingController;
  let api: PlaceApi;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(withInterceptors([credentialsInterceptor])),
        provideHttpClientTesting(),
      ],
    });

    httpTestingController = TestBed.inject(HttpTestingController);
    api = TestBed.inject(PlaceApi);
  });

  afterEach(() => {
    httpTestingController.verify();
  });

  it('searches places through the BFF place facade with browser credentials', () => {
    api.searchPlaces({
      query: 'quan',
      city: 'Ho Chi Minh City',
      area: 'District 1',
      cuisineType: 'Vietnamese',
      priceRange: '$$',
      limit: 10,
    }).subscribe((response) => {
      expect(response).toEqual(searchResponse);
    });

    const request = httpTestingController.expectOne('/api/places/search?query=quan&city=Ho%20Chi%20Minh%20City&area=District%201&cuisineType=Vietnamese&priceRange=$$&limit=10');
    expect(request.request.method).toBe('GET');
    expect(request.request.withCredentials).toBe(true);

    request.flush(searchResponse);
  });

  it('omits blank search parameters and includes explicit inactive flag', () => {
    api.searchPlaces({
      query: '   ',
      city: '',
      area: null,
      cuisineType: undefined,
      priceRange: '  ',
      includeInactive: true,
      limit: null,
    }).subscribe((response) => {
      expect(response).toEqual(searchResponse);
    });

    const request = httpTestingController.expectOne('/api/places/search?includeInactive=true');
    expect(request.request.method).toBe('GET');
    expect(request.request.withCredentials).toBe(true);

    request.flush(searchResponse);
  });

  it('trims non-blank search parameters before sending them', () => {
    api.searchPlaces({
      query: '  bun cha  ',
      city: '  Hanoi  ',
      area: '  Hai Ba Trung  ',
      cuisineType: '  Vietnamese  ',
      priceRange: '  $$  ',
      limit: 5,
    }).subscribe((response) => {
      expect(response).toEqual(searchResponse);
    });

    const request = httpTestingController.expectOne('/api/places/search?query=bun%20cha&city=Hanoi&area=Hai%20Ba%20Trung&cuisineType=Vietnamese&priceRange=$$&limit=5');
    expect(request.request.method).toBe('GET');

    request.flush(searchResponse);
  });

  it('loads a place detail through the BFF place facade', () => {
    const placeId = '22222222-2222-2222-2222-222222222222';

    api.getPlace(placeId).subscribe((place) => {
      expect(place.id).toBe(placeId);
      expect(place.name).toBe('Bún Chả Hương Liên');
    });

    const request = httpTestingController.expectOne(`/api/places/${placeId}`);
    expect(request.request.method).toBe('GET');
    expect(request.request.withCredentials).toBe(true);

    request.flush({
      id: placeId,
      name: 'Bún Chả Hương Liên',
      normalizedName: 'bun cha huong lien',
      description: 'Famous grilled pork noodles',
      addressText: '24 Lê Văn Hưu',
      area: 'Hai Bà Trưng',
      city: 'Hanoi',
      country: 'Vietnam',
      latitude: 21.0184,
      longitude: 105.8542,
      cuisineTypes: ['vietnamese'],
      priceRange: '$$',
      status: 'Active',
      createdAtUtc: '2026-04-27T00:00:00Z',
      updatedAtUtc: '2026-04-27T00:00:00Z',
    });
  });
});

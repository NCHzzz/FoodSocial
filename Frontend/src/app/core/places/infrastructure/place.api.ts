import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';

import { PlaceDetailResponse, PlaceSearchRequest, PlaceSearchResponse } from '../place.models';

@Injectable({ providedIn: 'root' })
export class PlaceApi {
  private readonly httpClient = inject(HttpClient);

  private static readonly placesEndpoint = '/api/places';

  searchPlaces(request: PlaceSearchRequest): Observable<PlaceSearchResponse> {
    return this.httpClient.get<PlaceSearchResponse>(`${PlaceApi.placesEndpoint}/search`, {
      params: this.buildSearchParams(request),
    });
  }

  getPlace(placeId: string): Observable<PlaceDetailResponse> {
    return this.httpClient.get<PlaceDetailResponse>(`${PlaceApi.placesEndpoint}/${encodeURIComponent(placeId)}`, {
    });
  }

  private buildSearchParams(request: PlaceSearchRequest): HttpParams {
    let params = new HttpParams();

    if (!this.isBlank(request.query)) {
      params = params.set('query', request.query.trim());
    }

    if (!this.isBlank(request.city)) {
      params = params.set('city', request.city.trim());
    }

    if (!this.isBlank(request.area)) {
      params = params.set('area', request.area.trim());
    }

    if (!this.isBlank(request.cuisineType)) {
      params = params.set('cuisineType', request.cuisineType.trim());
    }

    if (!this.isBlank(request.priceRange)) {
      params = params.set('priceRange', request.priceRange.trim());
    }

    if (request.includeInactive === true) {
      params = params.set('includeInactive', 'true');
    }

    if (request.limit !== null && request.limit !== undefined) {
      params = params.set('limit', request.limit.toString());
    }

    return params;
  }

  private isBlank(value: string | null | undefined): value is null | undefined {
    return value === null || value === undefined || value.trim().length === 0;
  }
}

import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';

import { DishDetailResponse, DishSearchRequest, DishSearchResponse } from '../dish.models';

@Injectable({ providedIn: 'root' })
export class DishApi {
  private readonly httpClient = inject(HttpClient);

  private static readonly dishesEndpoint = '/api/dishes';

  searchDishes(request: DishSearchRequest): Observable<DishSearchResponse> {
    return this.httpClient.get<DishSearchResponse>(`${DishApi.dishesEndpoint}/search`, {
      params: this.buildSearchParams(request),
    });
  }

  getDish(dishId: string): Observable<DishDetailResponse> {
    return this.httpClient.get<DishDetailResponse>(`${DishApi.dishesEndpoint}/${encodeURIComponent(dishId)}`, {
    });
  }

  private buildSearchParams(request: DishSearchRequest): HttpParams {
    let params = new HttpParams();

    if (!this.isBlank(request.query)) {
      params = params.set('query', request.query.trim());
    }

    if (!this.isBlank(request.category)) {
      params = params.set('category', request.category.trim());
    }

    if (!this.isBlank(request.cuisineType)) {
      params = params.set('cuisineType', request.cuisineType.trim());
    }

    if (request.includeDeprecated === true) {
      params = params.set('includeDeprecated', 'true');
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

import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';

import { SearchAssistRequest, SearchAssistResponse } from '../search-assist.models';

@Injectable({ providedIn: 'root' })
export class SearchAssistApi {
  private readonly httpClient = inject(HttpClient);

  assist(request: SearchAssistRequest): Observable<SearchAssistResponse> {
    return this.httpClient.post<SearchAssistResponse>('/api/search/assist', request, {
      withCredentials: true,
    });
  }
}

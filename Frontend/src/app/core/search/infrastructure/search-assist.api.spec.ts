import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { credentialsInterceptor } from '../../http/credentials.interceptor';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';

import { SearchAssistApi } from './search-assist.api';

describe('SearchAssistApi', () => {
  let api: SearchAssistApi;
  let httpTestingController: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(withInterceptors([credentialsInterceptor])),
        provideHttpClientTesting(),
      ],
    });

    api = TestBed.inject(SearchAssistApi);
    httpTestingController = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpTestingController.verify();
  });

  it('posts search assist requests through the BFF', () => {
    api.assist({ query: 'spicy noodles', location: 'District 1' }).subscribe((response) => {
      expect(response.fallbackUsed).toBe(true);
      expect(response.suggestions).toHaveLength(1);
    });

    const request = httpTestingController.expectOne('/api/search/assist');
    expect(request.request.method).toBe('POST');
    expect(request.request.withCredentials).toBe(true);
    expect(request.request.body).toEqual({ query: 'spicy noodles', location: 'District 1' });
    request.flush({
      normalizedQuery: 'spicy noodles',
      assistStatus: 'ai-disabled-fallback',
      fallbackUsed: true,
      suggestions: [{ type: 'dish', value: 'spicy noodles', reason: 'Search dishes.' }],
      filters: { location: 'District 1' },
      explanation: 'Deterministic fallback.',
    });
  });
});

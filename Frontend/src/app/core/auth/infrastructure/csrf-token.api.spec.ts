import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';

import { CsrfTokenApi } from './csrf-token.api';

describe('CsrfTokenApi', () => {
  let httpTestingController: HttpTestingController;
  let api: CsrfTokenApi;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
      ],
    });

    httpTestingController = TestBed.inject(HttpTestingController);
    api = TestBed.inject(CsrfTokenApi);
  });

  afterEach(() => {
    httpTestingController.verify();
  });

  it('builds a CSRF header option with browser credentials', () => {
    api.withCsrfHeader().subscribe((options) => {
      expect(options.withCredentials).toBe(true);
      expect(options.headers.get('X-XSRF-TOKEN')).toBe('csrf-token');
    });

    const request = httpTestingController.expectOne('/api/auth/csrf');
    expect(request.request.method).toBe('GET');
    expect(request.request.withCredentials).toBe(true);
    request.flush({ requestToken: 'csrf-token', headerName: 'X-XSRF-TOKEN' });
  });

  it('does not reuse a failed token request', () => {
    api.getToken().subscribe({
      next: () => expect.fail('expected the first token request to fail'),
      error: (error: unknown) => expect(error).toBeTruthy(),
    });

    const failedRequest = httpTestingController.expectOne('/api/auth/csrf');
    failedRequest.flush('csrf unavailable', { status: 503, statusText: 'Service Unavailable' });

    api.getToken().subscribe((token) => {
      expect(token.requestToken).toBe('fresh-token');
      expect(token.headerName).toBe('X-XSRF-TOKEN');
    });

    const retryRequest = httpTestingController.expectOne('/api/auth/csrf');
    retryRequest.flush({ requestToken: 'fresh-token', headerName: 'X-XSRF-TOKEN' });
  });

  it('rejects malformed token payloads and retries with a fresh request', () => {
    api.withCsrfHeader().subscribe({
      next: () => expect.fail('expected malformed token payload to fail'),
      error: (error: unknown) => expect(error).toBeTruthy(),
    });

    const invalidRequest = httpTestingController.expectOne('/api/auth/csrf');
    invalidRequest.flush({ requestToken: '', headerName: 'X-XSRF-TOKEN' });

    api.withCsrfHeader().subscribe((options) => {
      expect(options.headers.get('X-XSRF-TOKEN')).toBe('fresh-token');
    });

    const retryRequest = httpTestingController.expectOne('/api/auth/csrf');
    retryRequest.flush({ requestToken: 'fresh-token', headerName: 'X-XSRF-TOKEN' });
  });
});

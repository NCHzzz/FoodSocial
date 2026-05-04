import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';

import { UpdateUserProfileRequest, UserProfileResponse } from '../user-profile.models';
import { UserProfileApi } from './user-profile.api';

describe('UserProfileApi', () => {
  const identityId = '6f4ad594-e916-4f0c-9d08-b1043e15db4b';
  const profileResponse: UserProfileResponse = {
    identityId,
    displayName: 'Demo Food Explorer',
    avatarUrl: null,
    bio: 'Chasing great com tam and pho spots.',
    preferredCuisines: ['Vietnamese', 'Street food'],
    dislikedIngredients: ['Cilantro'],
    dietaryRestrictions: ['Peanut-free'],
    followerCount: 12,
    followingCount: 8,
  };

  let httpTestingController: HttpTestingController;
  let api: UserProfileApi;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
      ],
    });

    httpTestingController = TestBed.inject(HttpTestingController);
    api = TestBed.inject(UserProfileApi);
  });

  afterEach(() => {
    httpTestingController.verify();
  });

  it('loads a public profile through the BFF user facade with browser credentials', () => {
    api.getProfile(identityId).subscribe((profile) => {
      expect(profile).toEqual(profileResponse);
    });

    const request = httpTestingController.expectOne(`/api/users/${identityId}`);
    expect(request.request.method).toBe('GET');
    expect(request.request.withCredentials).toBe(true);

    request.flush(profileResponse);
  });

  it('keeps upstream profile failures visible to callers', () => {
    api.getProfile(identityId).subscribe({
      next: () => expect.fail('expected getProfile to surface the HTTP error'),
      error: (error: unknown) => {
        expect(error).toBeTruthy();
      },
    });

    const request = httpTestingController.expectOne(`/api/users/${identityId}`);
    request.flush({ title: 'Profile unavailable' }, { status: 503, statusText: 'Service Unavailable' });
  });

  it('updates the signed-in owner profile through the protected BFF facade', () => {
    const updateRequest: UpdateUserProfileRequest = {
      displayName: 'Updated Explorer',
      avatarUrl: null,
      bio: 'More bun bo hue, less guesswork.',
      preferredCuisines: ['Vietnamese'],
      dislikedIngredients: [],
      dietaryRestrictions: [],
    };

    api.updateProfile(identityId, updateRequest).subscribe((profile) => {
      expect(profile.displayName).toBe('Updated Explorer');
    });

    const csrfRequest = httpTestingController.expectOne('/api/auth/csrf');
    expect(csrfRequest.request.method).toBe('GET');
    expect(csrfRequest.request.withCredentials).toBe(true);
    csrfRequest.flush({ requestToken: 'csrf-token', headerName: 'X-XSRF-TOKEN' });

    const request = httpTestingController.expectOne(`/api/users/${identityId}`);
    expect(request.request.method).toBe('PATCH');
    expect(request.request.withCredentials).toBe(true);
    expect(request.request.headers.get('X-XSRF-TOKEN')).toBe('csrf-token');
    expect(request.request.body).toEqual(updateRequest);

    request.flush({ ...profileResponse, ...updateRequest });
  });

  it('sends the CSRF token header for follow mutations', () => {
    api.follow(identityId).subscribe((response) => {
      expect(response.following).toBe(true);
    });

    const csrfRequest = httpTestingController.expectOne('/api/auth/csrf');
    csrfRequest.flush({ requestToken: 'csrf-token', headerName: 'X-XSRF-TOKEN' });

    const request = httpTestingController.expectOne(`/api/users/${identityId}/follow`);
    expect(request.request.method).toBe('POST');
    expect(request.request.withCredentials).toBe(true);
    expect(request.request.headers.get('X-XSRF-TOKEN')).toBe('csrf-token');

    request.flush({ targetIdentityId: identityId, following: true });
  });

  it('sends the CSRF token header for unfollow mutations', () => {
    api.unfollow(identityId).subscribe((response) => {
      expect(response.following).toBe(false);
    });

    const csrfRequest = httpTestingController.expectOne('/api/auth/csrf');
    csrfRequest.flush({ requestToken: 'csrf-token', headerName: 'X-XSRF-TOKEN' });

    const request = httpTestingController.expectOne(`/api/users/${identityId}/follow`);
    expect(request.request.method).toBe('DELETE');
    expect(request.request.withCredentials).toBe(true);
    expect(request.request.headers.get('X-XSRF-TOKEN')).toBe('csrf-token');

    request.flush({ targetIdentityId: identityId, following: false });
  });

  it('sends the CSRF token header for save and unsave mutations', () => {
    api.saveDish(identityId, 'dish-123').subscribe((response) => {
      expect(response.saved).toBe(true);
    });

    const saveCsrfRequest = httpTestingController.expectOne('/api/auth/csrf');
    saveCsrfRequest.flush({ requestToken: 'save-token', headerName: 'X-XSRF-TOKEN' });

    const saveRequest = httpTestingController.expectOne(`/api/users/${identityId}/saved/dishes/dish-123`);
    expect(saveRequest.request.method).toBe('PUT');
    expect(saveRequest.request.withCredentials).toBe(true);
    expect(saveRequest.request.headers.get('X-XSRF-TOKEN')).toBe('save-token');
    saveRequest.flush({ targetId: 'dish-123', saved: true });

    api.unsaveDish(identityId, 'dish-123').subscribe((response) => {
      expect(response.saved).toBe(false);
    });

    const unsaveCsrfRequest = httpTestingController.expectOne('/api/auth/csrf');
    unsaveCsrfRequest.flush({ requestToken: 'unsave-token', headerName: 'X-XSRF-TOKEN' });

    const unsaveRequest = httpTestingController.expectOne(`/api/users/${identityId}/saved/dishes/dish-123`);
    expect(unsaveRequest.request.method).toBe('DELETE');
    expect(unsaveRequest.request.withCredentials).toBe(true);
    expect(unsaveRequest.request.headers.get('X-XSRF-TOKEN')).toBe('unsave-token');
    unsaveRequest.flush({ targetId: 'dish-123', saved: false });
  });

  it('encodes identity and target identifiers in BFF paths', () => {
    const encodedIdentityId = 'identity/with space';
    const encodedTargetId = 'place/with space';

    api.savePlace(encodedIdentityId, encodedTargetId).subscribe((response) => {
      expect(response.saved).toBe(true);
    });

    const csrfRequest = httpTestingController.expectOne('/api/auth/csrf');
    csrfRequest.flush({ requestToken: 'csrf-token', headerName: 'X-XSRF-TOKEN' });

    const request = httpTestingController.expectOne('/api/users/identity%2Fwith%20space/saved/places/place%2Fwith%20space');
    expect(request.request.method).toBe('PUT');
    request.flush({ targetId: encodedTargetId, saved: true });
  });
});

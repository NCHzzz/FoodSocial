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

    const request = httpTestingController.expectOne(`/api/users/${identityId}`);
    expect(request.request.method).toBe('PATCH');
    expect(request.request.withCredentials).toBe(true);
    expect(request.request.body).toEqual(updateRequest);

    request.flush({ ...profileResponse, ...updateRequest });
  });
});

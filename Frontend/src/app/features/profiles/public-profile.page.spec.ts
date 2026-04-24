import { TestBed } from '@angular/core/testing';
import { ActivatedRoute } from '@angular/router';
import { Observable, of, throwError } from 'rxjs';

import { UserProfileApi } from '../../core/user-profiles/infrastructure/user-profile.api';
import { UserProfileResponse } from '../../core/user-profiles/user-profile.models';
import { PublicProfilePage } from './public-profile.page';

describe('PublicProfilePage', () => {
  const identityId = '6f4ad594-e916-4f0c-9d08-b1043e15db4b';
  const profile: UserProfileResponse = {
    identityId,
    displayName: 'Demo Food Explorer',
    avatarUrl: null,
    bio: 'Chasing great com tam and pho spots.',
    preferredCuisines: ['Vietnamese', 'Street food'],
    dislikedIngredients: ['Cilantro'],
    dietaryRestrictions: [],
    followerCount: 12,
    followingCount: 8,
  };

  const configurePage = async (getProfile: () => Observable<UserProfileResponse>) => {
    await TestBed.configureTestingModule({
      imports: [PublicProfilePage],
      providers: [
        {
          provide: ActivatedRoute,
          useValue: {
            snapshot: {
              paramMap: {
                get: (name: string) => name === 'identityId' ? identityId : null,
              },
            },
          },
        },
        {
          provide: UserProfileApi,
          useValue: {
            getProfile,
          },
        },
      ],
    }).compileComponents();
  };

  afterEach(() => {
    TestBed.resetTestingModule();
  });

  it('loads and renders the public profile identity and taste signals', async () => {
    await configurePage(() => of(profile));

    const fixture = TestBed.createComponent(PublicProfilePage);
    await fixture.whenStable();
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;

    expect(compiled.textContent).toContain('Demo Food Explorer');
    expect(compiled.textContent).toContain('12 followers');
    expect(compiled.textContent).toContain('8 following');
    expect(compiled.textContent).toContain('Vietnamese');
    expect(compiled.textContent).toContain('Cilantro');
  });

  it('shows a retryable profile state when the BFF facade fails', async () => {
    await configurePage(() => throwError(() => new Error('BFF unavailable')));

    const fixture = TestBed.createComponent(PublicProfilePage);
    await fixture.whenStable();
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;

    expect(compiled.textContent).toContain('Profile could not load');
    expect(compiled.textContent).toContain('Try again from Profiles');
  });
});

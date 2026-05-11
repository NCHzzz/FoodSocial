import { TestBed } from '@angular/core/testing';
import { ActivatedRoute, Router } from '@angular/router';
import { vi } from 'vitest';

import { ProfilesPage } from './profiles.page';

describe('ProfilesPage', () => {
  it('opens public profile by identity link', async () => {
    const navigateMock = vi.fn();

    await TestBed.configureTestingModule({
      imports: [ProfilesPage],
      providers: [
        { provide: Router, useValue: { navigate: navigateMock } },
        { provide: ActivatedRoute, useValue: { snapshot: { paramMap: { get: () => null } } } },
      ],
    }).compileComponents();

    const fixture = TestBed.createComponent(ProfilesPage);
    fixture.detectChanges();

    fixture.componentInstance['profileLookupForm'].controls.identityId.setValue('6f4ad594-e916-4f0c-9d08-b1043e15db4b');
    fixture.componentInstance['openPublicProfile']();

    expect(navigateMock).toHaveBeenCalledWith(['/profiles', '6f4ad594-e916-4f0c-9d08-b1043e15db4b']);
  });
});

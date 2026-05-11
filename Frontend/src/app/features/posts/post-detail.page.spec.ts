import { TestBed } from '@angular/core/testing';
import { ActivatedRoute, provideRouter } from '@angular/router';
import { of } from 'rxjs';
import { vi } from 'vitest';

import { AuthSessionState } from '../../core/auth/auth-session.models';
import { AuthSessionService } from '../../core/auth/auth-session.service';
import { BROWSER_WINDOW } from '../../core/auth/browser-window.token';
import { PostApi } from '../../core/posts/infrastructure/post.api';
import { PostDetailResponse } from '../../core/posts/post.models';
import { UserProfileApi } from '../../core/user-profiles/infrastructure/user-profile.api';
import { PostDetailPage } from './post-detail.page';

describe('PostDetailPage', () => {
  const post: PostDetailResponse = {
    id: 'post-1',
    authorId: 'author-1',
    dishId: 'dish-1',
    placeId: null,
    caption: 'Great bowl of pho.',
    rating: 5,
    priceRange: '$$',
    tags: ['pho'],
    locationText: 'District 1',
    status: 'Published',
    isHidden: false,
    likeCount: 1,
    repostCount: 0,
    commentCount: 0,
    media: [{ id: 'media-1', url: 'https://example.com/pho.jpg', type: 'image', thumbnailUrl: null, caption: null, isPrimary: true, sortOrder: 0 }],
    comments: [],
    createdAtUtc: '2026-05-04T00:00:00Z',
    updatedAtUtc: '2026-05-04T00:00:00Z',
  };

  const authState: AuthSessionState = {
    loadStatus: 'authenticated',
    session: {
      user: { subject: 'subject-1', preferredUsername: 'demo', email: 'demo@example.com', roles: ['user'] },
      identity: { identityId: 'identity-1', state: 'Active', bootstrapPending: false, downstreamBootstrapPending: false },
      serverTimeUtc: '2026-05-04T00:00:00Z',
    },
    isLoggingOut: false,
    lastLoadFailed: false,
  };

  it('submits report reason and description from explicit controls', async () => {
    const reportPostMock = vi.fn(() => of({}));

    await TestBed.configureTestingModule({
      imports: [PostDetailPage],
      providers: [
        provideRouter([]),
        {
          provide: ActivatedRoute,
          useValue: { snapshot: { paramMap: { get: () => 'post-1' } } },
        },
        {
          provide: AuthSessionService,
          useValue: { authState: () => authState, startLogin: vi.fn() },
        },
        {
          provide: BROWSER_WINDOW,
          useValue: { location: { origin: 'https://example.com' }, navigator: { clipboard: { writeText: vi.fn() } } },
        },
        {
          provide: PostApi,
          useValue: {
            getPost: () => of(post),
            likePost: () => of({ likeCount: 1 }),
            repostPost: () => of({ repostCount: 0 }),
            reportPost: reportPostMock,
            createComment: () => of({}),
          },
        },
        {
          provide: UserProfileApi,
          useValue: { savePost: () => of({}) },
        },
      ],
    }).compileComponents();

    const fixture = TestBed.createComponent(PostDetailPage);
    await fixture.whenStable();
    fixture.detectChanges();

    fixture.componentInstance['reportReasonControl'].setValue('unsafe_or_harmful');
    fixture.componentInstance['reportDescriptionControl'].setValue('Unsafe prep method.');
    fixture.componentInstance['reportPost']();

    expect(reportPostMock).toHaveBeenCalledWith('post-1', 'unsafe_or_harmful', 'Unsafe prep method.');
  });
});

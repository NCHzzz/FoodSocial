import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';

import { AdminReportsResponse, CreatePostRequest, DiscoverFeedResponse, PostDetailResponse, PostSearchResponse } from '../post.models';
import { PostApi } from './post.api';

describe('PostApi', () => {
  const postId = '11111111-1111-1111-1111-111111111111';
  const postDetail: PostDetailResponse = {
    id: postId,
    authorId: '22222222-2222-2222-2222-222222222222',
    dishId: '33333333-3333-3333-3333-333333333333',
    placeId: null,
    caption: 'Great phở broth.',
    rating: 5,
    priceRange: '$$',
    tags: ['pho'],
    locationText: 'District 1',
    status: 'Published',
    isHidden: false,
    likeCount: 2,
    repostCount: 0,
    commentCount: 1,
    media: [
      {
        id: '44444444-4444-4444-4444-444444444444',
        url: 'https://example.com/pho.jpg',
        type: 'image',
        thumbnailUrl: null,
        caption: null,
        isPrimary: true,
        sortOrder: 0,
      },
    ],
    comments: [],
    createdAtUtc: '2026-05-04T00:00:00Z',
    updatedAtUtc: '2026-05-04T00:00:00Z',
  };
  const feedResponse: DiscoverFeedResponse = {
    items: [
      {
        id: postId,
        authorId: postDetail.authorId,
        dishId: postDetail.dishId,
        placeId: null,
        caption: postDetail.caption,
        rating: 5,
        priceRange: '$$',
        tags: ['pho'],
        primaryMediaUrl: 'https://example.com/pho.jpg',
        isHidden: false,
        likeCount: 2,
        repostCount: 0,
        commentCount: 1,
        createdAtUtc: '2026-05-04T00:00:00Z',
      },
    ],
    limit: 12,
  };

  let httpTestingController: HttpTestingController;
  let api: PostApi;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
      ],
    });

    httpTestingController = TestBed.inject(HttpTestingController);
    api = TestBed.inject(PostApi);
  });

  afterEach(() => {
    httpTestingController.verify();
  });

  it('loads Discover feed through the BFF with browser credentials', () => {
    api.getDiscoverFeed(12).subscribe((response) => {
      expect(response).toEqual(feedResponse);
    });

    const request = httpTestingController.expectOne('/api/feed/discover?limit=12');
    expect(request.request.method).toBe('GET');
    expect(request.request.withCredentials).toBe(true);

    request.flush(feedResponse);
  });

  it('searches posts with trimmed bounded query parameters', () => {
    const response: PostSearchResponse = { items: feedResponse.items, limit: 24 };

    api.searchPosts({ query: '  pho  ', tag: ' soup ', authorId: '', dishId: null, placeId: undefined, limit: 24 }).subscribe((result) => {
      expect(result).toEqual(response);
    });

    const request = httpTestingController.expectOne('/api/posts/search?tag=soup&query=pho&limit=24');
    expect(request.request.method).toBe('GET');
    expect(request.request.withCredentials).toBe(true);

    request.flush(response);
  });

  it('loads post detail through the BFF', () => {
    api.getPost(postId).subscribe((post) => {
      expect(post).toEqual(postDetail);
    });

    const request = httpTestingController.expectOne(`/api/posts/${postId}`);
    expect(request.request.method).toBe('GET');
    expect(request.request.withCredentials).toBe(true);

    request.flush(postDetail);
  });


  it('uploads media through CSRF-gated BFF route', () => {
    const file = new File(['image-bytes'], 'pho.jpg', { type: 'image/jpeg' });

    api.uploadMedia(file).subscribe((response) => {
      expect(response.url).toBe('/api/media/uploads/11111111-1111-1111-1111-111111111111/original');
    });

    httpTestingController.expectOne('/api/auth/csrf').flush({ requestToken: 'upload-token', headerName: 'X-XSRF-TOKEN' });
    const request = httpTestingController.expectOne('/api/media/uploads');
    expect(request.request.method).toBe('POST');
    expect(request.request.withCredentials).toBe(true);
    expect(request.request.headers.get('X-XSRF-TOKEN')).toBe('upload-token');
    expect(request.request.body instanceof FormData).toBe(true);
    request.flush({
      uploadId: '11111111-1111-1111-1111-111111111111',
      url: '/api/media/uploads/11111111-1111-1111-1111-111111111111/original',
      thumbnailUrl: '/api/media/uploads/11111111-1111-1111-1111-111111111111/thumbnail',
      contentType: 'image/jpeg',
      sizeBytes: 10,
      cleanupDueAtUtc: '2026-05-12T00:00:00Z',
      status: 'StoredWithDerivativePlaceholder',
    });
  });

  it('creates a post with CSRF-gated browser credentials', () => {
    const createRequest: CreatePostRequest = {
      authorId: '00000000-0000-0000-0000-000000000000',
      dishId: postDetail.dishId,
      caption: postDetail.caption,
      media: [{ url: 'https://example.com/pho.jpg', type: 'image', thumbnailUrl: null, caption: null, isPrimary: true }],
      placeId: null,
      rating: 5,
      priceRange: '$$',
      tags: ['pho'],
      locationText: 'District 1',
    };

    api.createPost(createRequest).subscribe((post) => {
      expect(post).toEqual(postDetail);
    });

    const csrfRequest = httpTestingController.expectOne('/api/auth/csrf');
    csrfRequest.flush({ requestToken: 'csrf-token', headerName: 'X-XSRF-TOKEN' });

    const request = httpTestingController.expectOne('/api/posts');
    expect(request.request.method).toBe('POST');
    expect(request.request.withCredentials).toBe(true);
    expect(request.request.headers.get('X-XSRF-TOKEN')).toBe('csrf-token');
    expect(request.request.body).toEqual(createRequest);

    request.flush(postDetail);
  });


  it('loads and resolves admin reports through CSRF-gated BFF moderation routes', () => {
    const reportResponse: AdminReportsResponse = {
      items: [
        {
          id: '66666666-6666-6666-6666-666666666666',
          targetType: 'Post',
          targetId: postId,
          reporterId: postDetail.authorId,
          reason: 'spam_or_low_quality',
          description: 'Looks suspicious',
          status: 'Submitted',
          moderatorId: null,
          resolutionNote: null,
          createdAtUtc: '2026-05-04T00:00:00Z',
          resolvedAtUtc: null,
        },
      ],
      limit: 20,
    };

    api.getAdminReports(20).subscribe((response) => {
      expect(response).toEqual(reportResponse);
    });

    const adminRequest = httpTestingController.expectOne('/api/admin/reports?limit=20');
    expect(adminRequest.request.method).toBe('GET');
    expect(adminRequest.request.withCredentials).toBe(true);
    adminRequest.flush(reportResponse);

    api.resolveReport(reportResponse.items[0].id, {
      moderatorId: '00000000-0000-0000-0000-000000000000',
      action: 'dismiss',
      auditNote: 'Reviewed manually.',
    }).subscribe((response) => {
      expect(response.status).toBe('ResolvedNoAction');
    });

    httpTestingController.expectOne('/api/auth/csrf').flush({ requestToken: 'moderation-token', headerName: 'X-XSRF-TOKEN' });
    const resolveRequest = httpTestingController.expectOne(`/api/admin/reports/${reportResponse.items[0].id}/resolve`);
    expect(resolveRequest.request.method).toBe('POST');
    expect(resolveRequest.request.withCredentials).toBe(true);
    expect(resolveRequest.request.headers.get('X-XSRF-TOKEN')).toBe('moderation-token');
    expect(resolveRequest.request.body.action).toBe('dismiss');
    resolveRequest.flush({
      ...reportResponse.items[0],
      status: 'ResolvedNoAction',
      moderatorId: postDetail.authorId,
      resolutionNote: 'Reviewed manually.',
      resolvedAtUtc: '2026-05-04T01:00:00Z',
    });
  });


  it('reposts and reads notifications through CSRF-gated BFF routes', () => {
    api.repostPost(postId, { actorId: '00000000-0000-0000-0000-000000000000', caption: null }).subscribe((response) => {
      expect(response.repostCount).toBe(1);
    });
    httpTestingController.expectOne('/api/auth/csrf').flush({ requestToken: 'repost-token', headerName: 'X-XSRF-TOKEN' });
    const repostRequest = httpTestingController.expectOne(`/api/posts/${postId}/repost`);
    expect(repostRequest.request.method).toBe('POST');
    expect(repostRequest.request.headers.get('X-XSRF-TOKEN')).toBe('repost-token');
    repostRequest.flush({ id: '77777777-7777-7777-7777-777777777777', postId, actorId: postDetail.authorId, caption: null, repostCount: 1, createdAtUtc: '2026-05-04T00:00:00Z' });

    api.getNotifications(20).subscribe((response) => {
      expect(response.unreadCount).toBe(1);
    });
    const notificationsRequest = httpTestingController.expectOne('/api/me/notifications?limit=20');
    expect(notificationsRequest.request.method).toBe('GET');
    expect(notificationsRequest.request.withCredentials).toBe(true);
    notificationsRequest.flush({
      items: [{
        id: '88888888-8888-8888-8888-888888888888',
        recipientId: postDetail.authorId,
        actorId: '99999999-9999-9999-9999-999999999999',
        type: 'PostReposted',
        targetType: 'Repost',
        targetId: '77777777-7777-7777-7777-777777777777',
        postId,
        message: 'reposted your food post',
        isRead: false,
        createdAtUtc: '2026-05-04T00:00:00Z',
        readAtUtc: null,
      }],
      limit: 20,
      unreadCount: 1,
    });

    api.markNotificationRead('88888888-8888-8888-8888-888888888888').subscribe((response) => {
      expect(response.isRead).toBe(true);
    });
    httpTestingController.expectOne('/api/auth/csrf').flush({ requestToken: 'notification-token', headerName: 'X-XSRF-TOKEN' });
    const readRequest = httpTestingController.expectOne('/api/notifications/88888888-8888-8888-8888-888888888888/read');
    expect(readRequest.request.method).toBe('PATCH');
    expect(readRequest.request.headers.get('X-XSRF-TOKEN')).toBe('notification-token');
    readRequest.flush({
      id: '88888888-8888-8888-8888-888888888888',
      recipientId: postDetail.authorId,
      actorId: '99999999-9999-9999-9999-999999999999',
      type: 'PostReposted',
      targetType: 'Repost',
      targetId: '77777777-7777-7777-7777-777777777777',
      postId,
      message: 'reposted your food post',
      isRead: true,
      createdAtUtc: '2026-05-04T00:00:00Z',
      readAtUtc: '2026-05-04T01:00:00Z',
    });
  });

  it('sends CSRF-gated like, comment, and report mutations', () => {
    api.likePost(postId).subscribe((response) => {
      expect(response.likeCount).toBe(3);
    });
    httpTestingController.expectOne('/api/auth/csrf').flush({ requestToken: 'like-token', headerName: 'X-XSRF-TOKEN' });
    const likeRequest = httpTestingController.expectOne(`/api/posts/${postId}/likes`);
    expect(likeRequest.request.method).toBe('POST');
    expect(likeRequest.request.headers.get('X-XSRF-TOKEN')).toBe('like-token');
    likeRequest.flush({ postId, actorId: postDetail.authorId, liked: true, likeCount: 3 });

    api.createComment(postId, 'Looks great').subscribe((response) => {
      expect(response).toBeTruthy();
    });
    httpTestingController.expectOne('/api/auth/csrf').flush({ requestToken: 'comment-token', headerName: 'X-XSRF-TOKEN' });
    const commentRequest = httpTestingController.expectOne(`/api/posts/${postId}/comments`);
    expect(commentRequest.request.method).toBe('POST');
    expect(commentRequest.request.body.body).toBe('Looks great');
    expect(commentRequest.request.body.authorId).toBe('00000000-0000-0000-0000-000000000000');
    commentRequest.flush({ id: '55555555-5555-5555-5555-555555555555' });

    api.reportPost(postId, 'spam_or_low_quality', 'Looks suspicious').subscribe((response) => {
      expect(response.status).toBe('Submitted');
    });
    httpTestingController.expectOne('/api/auth/csrf').flush({ requestToken: 'report-token', headerName: 'X-XSRF-TOKEN' });
    const reportRequest = httpTestingController.expectOne(`/api/posts/${postId}/report`);
    expect(reportRequest.request.method).toBe('POST');
    expect(reportRequest.request.body.reason).toBe('spam_or_low_quality');
    expect(reportRequest.request.body.reporterId).toBe('00000000-0000-0000-0000-000000000000');
    reportRequest.flush({
      id: '66666666-6666-6666-6666-666666666666',
      targetType: 'Post',
      targetId: postId,
      reporterId: postDetail.authorId,
      reason: 'spam_or_low_quality',
      description: 'Looks suspicious',
      status: 'Submitted',
      moderatorId: null,
      resolutionNote: null,
      createdAtUtc: '2026-05-04T00:00:00Z',
      resolvedAtUtc: null,
    });
  });
});

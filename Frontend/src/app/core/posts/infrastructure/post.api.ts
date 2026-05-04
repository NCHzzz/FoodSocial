import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable, switchMap } from 'rxjs';

import { CsrfTokenApi } from '../../auth/infrastructure/csrf-token.api';
import {
  AdminReportsResponse,
  ContentReportResponse,
  CreateContentReportRequest,
  CreatePostCommentRequest,
  CreatePostRequest,
  CreatePostRepostRequest,
  DiscoverFeedResponse,
  NotificationListResponse,
  MediaUploadResponse,
  NotificationResponse,
  PostDetailResponse,
  PostLikeResponse,
  PostRepostResponse,
  PostSearchRequest,
  PostSearchResponse,
  ResolveContentReportRequest,
} from '../post.models';

@Injectable({ providedIn: 'root' })
export class PostApi {
  private readonly httpClient = inject(HttpClient);
  private readonly csrfTokenApi = inject(CsrfTokenApi);

  private static readonly postsEndpoint = '/api/posts';
  private static readonly discoverFeedEndpoint = '/api/feed/discover';

  getDiscoverFeed(limit: number | null = null): Observable<DiscoverFeedResponse> {
    return this.httpClient.get<DiscoverFeedResponse>(PostApi.discoverFeedEndpoint, {
      params: this.buildLimitParams(limit),
      withCredentials: true,
    });
  }

  searchPosts(request: PostSearchRequest): Observable<PostSearchResponse> {
    return this.httpClient.get<PostSearchResponse>(`${PostApi.postsEndpoint}/search`, {
      params: this.buildSearchParams(request),
      withCredentials: true,
    });
  }

  getPost(postId: string): Observable<PostDetailResponse> {
    return this.httpClient.get<PostDetailResponse>(this.buildPostEndpoint(postId), {
      withCredentials: true,
    });
  }

  createPost(request: CreatePostRequest): Observable<PostDetailResponse> {
    return this.csrfTokenApi.withCsrfHeader().pipe(
      switchMap((options) => this.httpClient.post<PostDetailResponse>(PostApi.postsEndpoint, request, options)),
    );
  }

  uploadMedia(file: File): Observable<MediaUploadResponse> {
    const formData = new FormData();
    formData.append('file', file, file.name);
    return this.csrfTokenApi.withCsrfHeader().pipe(
      switchMap((options) => this.httpClient.post<MediaUploadResponse>('/api/media/uploads', formData, options)),
    );
  }

  likePost(postId: string): Observable<PostLikeResponse> {
    return this.csrfTokenApi.withCsrfHeader().pipe(
      switchMap((options) => this.httpClient.post<PostLikeResponse>(`${this.buildPostEndpoint(postId)}/likes`, null, options)),
    );
  }

  unlikePost(postId: string): Observable<PostLikeResponse> {
    return this.csrfTokenApi.withCsrfHeader().pipe(
      switchMap((options) => this.httpClient.delete<PostLikeResponse>(`${this.buildPostEndpoint(postId)}/likes`, options)),
    );
  }

  repostPost(postId: string, request: CreatePostRepostRequest): Observable<PostRepostResponse> {
    return this.csrfTokenApi.withCsrfHeader().pipe(
      switchMap((options) => this.httpClient.post<PostRepostResponse>(`${this.buildPostEndpoint(postId)}/repost`, request, options)),
    );
  }

  createComment(postId: string, body: string): Observable<unknown> {
    const request: CreatePostCommentRequest = {
      authorId: PostApi.emptyGuid,
      body,
    };

    return this.csrfTokenApi.withCsrfHeader().pipe(
      switchMap((options) => this.httpClient.post(`${this.buildPostEndpoint(postId)}/comments`, request, options)),
    );
  }


  getNotifications(limit: number | null = null): Observable<NotificationListResponse> {
    return this.httpClient.get<NotificationListResponse>('/api/me/notifications', {
      params: this.buildLimitParams(limit),
      withCredentials: true,
    });
  }

  markNotificationRead(notificationId: string): Observable<NotificationResponse> {
    return this.csrfTokenApi.withCsrfHeader().pipe(
      switchMap((options) => this.httpClient.patch<NotificationResponse>(`/api/notifications/${encodeURIComponent(notificationId)}/read`, null, options)),
    );
  }

  getAdminReports(limit: number | null = null): Observable<AdminReportsResponse> {
    return this.httpClient.get<AdminReportsResponse>('/api/admin/reports', {
      params: this.buildLimitParams(limit),
      withCredentials: true,
    });
  }

  resolveReport(reportId: string, request: ResolveContentReportRequest): Observable<ContentReportResponse> {
    return this.csrfTokenApi.withCsrfHeader().pipe(
      switchMap((options) => this.httpClient.post<ContentReportResponse>(`/api/admin/reports/${encodeURIComponent(reportId)}/resolve`, request, options)),
    );
  }

  reportPost(postId: string, reason: string, description: string | null = null): Observable<ContentReportResponse> {
    const request: CreateContentReportRequest = {
      reporterId: PostApi.emptyGuid,
      reason,
      description,
    };

    return this.csrfTokenApi.withCsrfHeader().pipe(
      switchMap((options) => this.httpClient.post<ContentReportResponse>(`${this.buildPostEndpoint(postId)}/report`, request, options)),
    );
  }

  private buildPostEndpoint(postId: string): string {
    return `${PostApi.postsEndpoint}/${encodeURIComponent(postId)}`;
  }

  private buildSearchParams(request: PostSearchRequest): HttpParams {
    let params = new HttpParams();

    if (!this.isBlank(request.authorId)) {
      params = params.set('authorId', request.authorId.trim());
    }

    if (!this.isBlank(request.dishId)) {
      params = params.set('dishId', request.dishId.trim());
    }

    if (!this.isBlank(request.placeId)) {
      params = params.set('placeId', request.placeId.trim());
    }

    if (!this.isBlank(request.tag)) {
      params = params.set('tag', request.tag.trim());
    }

    if (!this.isBlank(request.query)) {
      params = params.set('query', request.query.trim());
    }

    if (request.limit !== null && request.limit !== undefined) {
      params = params.set('limit', request.limit.toString());
    }

    return params;
  }

  private buildLimitParams(limit: number | null): HttpParams {
    return limit === null ? new HttpParams() : new HttpParams().set('limit', limit.toString());
  }

  private isBlank(value: string | null | undefined): value is null | undefined {
    return value === null || value === undefined || value.trim().length === 0;
  }

  private static readonly emptyGuid = '00000000-0000-0000-0000-000000000000';
}

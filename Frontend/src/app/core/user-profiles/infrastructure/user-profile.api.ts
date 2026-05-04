import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable, switchMap } from 'rxjs';

import { CsrfTokenApi } from '../../auth/infrastructure/csrf-token.api';

import {
  FollowUserResponse,
  SaveUserItemResponse,
  UpdateUserProfileRequest,
  UserProfileListResponse,
  UserNotificationListResponse,
  UserNotificationResponse,
  UserProfileResponse,
  UserSavedItemsResponse,
} from '../user-profile.models';

type SavedItemKind = 'posts' | 'dishes' | 'places';

@Injectable({ providedIn: 'root' })
export class UserProfileApi {
  private readonly httpClient = inject(HttpClient);
  private readonly csrfTokenApi = inject(CsrfTokenApi);

  private static readonly usersEndpoint = '/api/users';

  getProfile(identityId: string): Observable<UserProfileResponse> {
    return this.httpClient.get<UserProfileResponse>(this.buildUserEndpoint(identityId), {
      withCredentials: true,
    });
  }

  updateProfile(identityId: string, request: UpdateUserProfileRequest): Observable<UserProfileResponse> {
    return this.csrfTokenApi.withCsrfHeader().pipe(
      switchMap((options) => this.httpClient.patch<UserProfileResponse>(this.buildUserEndpoint(identityId), request, options)),
    );
  }

  getFollowers(identityId: string): Observable<UserProfileListResponse> {
    return this.httpClient.get<UserProfileListResponse>(`${this.buildUserEndpoint(identityId)}/followers`, {
      withCredentials: true,
    });
  }

  getFollowing(identityId: string): Observable<UserProfileListResponse> {
    return this.httpClient.get<UserProfileListResponse>(`${this.buildUserEndpoint(identityId)}/following`, {
      withCredentials: true,
    });
  }

  follow(identityId: string): Observable<FollowUserResponse> {
    return this.csrfTokenApi.withCsrfHeader().pipe(
      switchMap((options) => this.httpClient.post<FollowUserResponse>(`${this.buildUserEndpoint(identityId)}/follow`, null, options)),
    );
  }

  unfollow(identityId: string): Observable<FollowUserResponse> {
    return this.csrfTokenApi.withCsrfHeader().pipe(
      switchMap((options) => this.httpClient.delete<FollowUserResponse>(`${this.buildUserEndpoint(identityId)}/follow`, options)),
    );
  }

  getSavedItems(identityId: string): Observable<UserSavedItemsResponse> {
    return this.httpClient.get<UserSavedItemsResponse>(`${this.buildUserEndpoint(identityId)}/saved`, {
      withCredentials: true,
    });
  }

  getNotifications(identityId: string): Observable<UserNotificationListResponse> {
    return this.httpClient.get<UserNotificationListResponse>(`${this.buildUserEndpoint(identityId)}/notifications`, {
      withCredentials: true,
    });
  }

  markNotificationRead(identityId: string, notificationId: string): Observable<UserNotificationResponse> {
    return this.csrfTokenApi.withCsrfHeader().pipe(
      switchMap((options) => this.httpClient.patch<UserNotificationResponse>(`${this.buildUserEndpoint(identityId)}/notifications/${encodeURIComponent(notificationId)}/read`, null, options)),
    );
  }

  savePost(identityId: string, postId: string): Observable<SaveUserItemResponse> {
    return this.saveItem(identityId, 'posts', postId);
  }

  unsavePost(identityId: string, postId: string): Observable<SaveUserItemResponse> {
    return this.unsaveItem(identityId, 'posts', postId);
  }

  saveDish(identityId: string, dishId: string): Observable<SaveUserItemResponse> {
    return this.saveItem(identityId, 'dishes', dishId);
  }

  unsaveDish(identityId: string, dishId: string): Observable<SaveUserItemResponse> {
    return this.unsaveItem(identityId, 'dishes', dishId);
  }

  savePlace(identityId: string, placeId: string): Observable<SaveUserItemResponse> {
    return this.saveItem(identityId, 'places', placeId);
  }

  unsavePlace(identityId: string, placeId: string): Observable<SaveUserItemResponse> {
    return this.unsaveItem(identityId, 'places', placeId);
  }

  private saveItem(identityId: string, kind: SavedItemKind, targetId: string): Observable<SaveUserItemResponse> {
    return this.csrfTokenApi.withCsrfHeader().pipe(
      switchMap((options) => this.httpClient.put<SaveUserItemResponse>(this.buildSavedItemEndpoint(identityId, kind, targetId), null, options)),
    );
  }

  private unsaveItem(identityId: string, kind: SavedItemKind, targetId: string): Observable<SaveUserItemResponse> {
    return this.csrfTokenApi.withCsrfHeader().pipe(
      switchMap((options) => this.httpClient.delete<SaveUserItemResponse>(this.buildSavedItemEndpoint(identityId, kind, targetId), options)),
    );
  }

  private buildUserEndpoint(identityId: string): string {
    return `${UserProfileApi.usersEndpoint}/${encodeURIComponent(identityId)}`;
  }

  private buildSavedItemEndpoint(identityId: string, kind: SavedItemKind, targetId: string): string {
    return `${this.buildUserEndpoint(identityId)}/saved/${kind}/${encodeURIComponent(targetId)}`;
  }
}

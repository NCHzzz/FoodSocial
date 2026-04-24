import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';

import {
  FollowUserResponse,
  SaveUserItemResponse,
  UpdateUserProfileRequest,
  UserProfileListResponse,
  UserProfileResponse,
  UserSavedItemsResponse,
} from '../user-profile.models';

type SavedItemKind = 'posts' | 'dishes' | 'places';

@Injectable({ providedIn: 'root' })
export class UserProfileApi {
  private readonly httpClient = inject(HttpClient);

  private static readonly usersEndpoint = '/api/users';

  getProfile(identityId: string): Observable<UserProfileResponse> {
    return this.httpClient.get<UserProfileResponse>(this.buildUserEndpoint(identityId), {
      withCredentials: true,
    });
  }

  updateProfile(identityId: string, request: UpdateUserProfileRequest): Observable<UserProfileResponse> {
    return this.httpClient.patch<UserProfileResponse>(this.buildUserEndpoint(identityId), request, {
      withCredentials: true,
    });
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
    return this.httpClient.post<FollowUserResponse>(`${this.buildUserEndpoint(identityId)}/follow`, null, {
      withCredentials: true,
    });
  }

  unfollow(identityId: string): Observable<FollowUserResponse> {
    return this.httpClient.delete<FollowUserResponse>(`${this.buildUserEndpoint(identityId)}/follow`, {
      withCredentials: true,
    });
  }

  getSavedItems(identityId: string): Observable<UserSavedItemsResponse> {
    return this.httpClient.get<UserSavedItemsResponse>(`${this.buildUserEndpoint(identityId)}/saved`, {
      withCredentials: true,
    });
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
    return this.httpClient.put<SaveUserItemResponse>(this.buildSavedItemEndpoint(identityId, kind, targetId), null, {
      withCredentials: true,
    });
  }

  private unsaveItem(identityId: string, kind: SavedItemKind, targetId: string): Observable<SaveUserItemResponse> {
    return this.httpClient.delete<SaveUserItemResponse>(this.buildSavedItemEndpoint(identityId, kind, targetId), {
      withCredentials: true,
    });
  }

  private buildUserEndpoint(identityId: string): string {
    return `${UserProfileApi.usersEndpoint}/${encodeURIComponent(identityId)}`;
  }

  private buildSavedItemEndpoint(identityId: string, kind: SavedItemKind, targetId: string): string {
    return `${this.buildUserEndpoint(identityId)}/saved/${kind}/${encodeURIComponent(targetId)}`;
  }
}

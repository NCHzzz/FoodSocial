export interface UserProfileResponse {
  identityId: string;
  displayName: string | null;
  avatarUrl: string | null;
  bio: string | null;
  preferredCuisines: string[];
  dislikedIngredients: string[];
  dietaryRestrictions: string[];
  followerCount: number;
  followingCount: number;
}

export interface UpdateUserProfileRequest {
  displayName: string | null;
  avatarUrl: string | null;
  bio: string | null;
  preferredCuisines: string[] | null;
  dislikedIngredients: string[] | null;
  dietaryRestrictions: string[] | null;
}

export interface UserProfileSummaryDto {
  identityId: string;
  displayName: string | null;
  avatarUrl: string | null;
}

export interface UserProfileListResponse {
  items: UserProfileSummaryDto[];
  totalCount: number;
}

export interface FollowUserResponse {
  targetIdentityId: string;
  following: boolean;
}

export interface SavedPostReferenceDto {
  postId: string;
  savedAtUtc: string;
}

export interface SavedDishReferenceDto {
  dishId: string;
  savedAtUtc: string;
}

export interface SavedPlaceReferenceDto {
  placeId: string;
  savedAtUtc: string;
}

export interface UserSavedItemsResponse {
  posts: SavedPostReferenceDto[];
  dishes: SavedDishReferenceDto[];
  places: SavedPlaceReferenceDto[];
}

export interface SaveUserItemResponse {
  userIdentityId: string;
  targetId: string;
  targetType: string;
  saved: boolean;
}


export interface UserNotificationResponse {
  id: string;
  recipientIdentityId: string;
  actorIdentityId: string;
  type: string;
  message: string;
  isRead: boolean;
  createdAtUtc: string;
  readAtUtc: string | null;
}

export interface UserNotificationListResponse {
  items: UserNotificationResponse[];
  limit: number;
  unreadCount: number;
}

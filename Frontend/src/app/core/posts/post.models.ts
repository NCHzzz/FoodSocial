export interface PostMediaRequest {
  url: string;
  type: string;
  thumbnailUrl: string | null;
  caption: string | null;
  isPrimary: boolean;
}

export interface CreatePostRequest {
  authorId: string;
  dishId: string;
  caption: string;
  media: PostMediaRequest[];
  placeId?: string | null;
  rating?: number | null;
  priceRange?: string | null;
  tags?: string[] | null;
  locationText?: string | null;
}

export interface PostMediaResponse {
  id: string;
  url: string;
  type: string;
  thumbnailUrl: string | null;
  caption: string | null;
  isPrimary: boolean;
  sortOrder: number;
}

export interface MediaUploadResponse {
  uploadId: string;
  url: string;
  thumbnailUrl: string;
  contentType: string;
  sizeBytes: number;
  cleanupDueAtUtc: string;
  status: string;
}

export interface PostCommentResponse {
  id: string;
  postId: string;
  authorId: string;
  body: string;
  status: string;
  createdAtUtc: string;
  updatedAtUtc: string;
}

export interface PostDetailResponse {
  id: string;
  authorId: string;
  dishId: string;
  placeId: string | null;
  caption: string;
  rating: number | null;
  priceRange: string | null;
  tags: string[];
  locationText: string | null;
  status: string;
  isHidden: boolean;
  likeCount: number;
  repostCount: number;
  commentCount: number;
  media: PostMediaResponse[];
  comments: PostCommentResponse[];
  createdAtUtc: string;
  updatedAtUtc: string;
}

export interface PostSummaryResponse {
  id: string;
  authorId: string;
  dishId: string;
  placeId: string | null;
  caption: string;
  rating: number | null;
  priceRange: string | null;
  tags: string[];
  primaryMediaUrl: string | null;
  isHidden: boolean;
  likeCount: number;
  repostCount: number;
  commentCount: number;
  createdAtUtc: string;
}

export interface PostSearchRequest {
  authorId?: string | null;
  dishId?: string | null;
  placeId?: string | null;
  tag?: string | null;
  query?: string | null;
  limit?: number | null;
}

export interface PostSearchResponse {
  items: PostSummaryResponse[];
  limit: number;
}

export interface DiscoverFeedResponse {
  items: PostSummaryResponse[];
  limit: number;
}

export interface PostLikeResponse {
  postId: string;
  actorId: string;
  liked: boolean;
  likeCount: number;
}

export interface CreatePostRepostRequest {
  actorId: string;
  caption: string | null;
}

export interface PostRepostResponse {
  id: string;
  postId: string;
  actorId: string;
  caption: string | null;
  repostCount: number;
  createdAtUtc: string;
}

export interface CreatePostCommentRequest {
  authorId: string;
  body: string;
}

export interface CreateContentReportRequest {
  reporterId: string;
  reason: string;
  description: string | null;
}

export interface ContentReportResponse {
  id: string;
  targetType: string;
  targetId: string;
  reporterId: string;
  reason: string;
  description: string | null;
  status: string;
  moderatorId: string | null;
  resolutionNote: string | null;
  createdAtUtc: string;
  resolvedAtUtc: string | null;
}


export interface AdminReportsResponse {
  items: ContentReportResponse[];
  limit: number;
}

export interface ResolveContentReportRequest {
  moderatorId: string;
  action: string;
  auditNote: string;
}


export interface NotificationResponse {
  id: string;
  recipientId: string;
  actorId: string;
  type: string;
  targetType: string;
  targetId: string;
  postId: string | null;
  message: string;
  isRead: boolean;
  createdAtUtc: string;
  readAtUtc: string | null;
}

export interface NotificationListResponse {
  items: NotificationResponse[];
  limit: number;
  unreadCount: number;
}

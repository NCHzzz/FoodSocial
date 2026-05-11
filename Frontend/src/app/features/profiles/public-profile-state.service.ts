import { computed, effect, inject, Injectable, signal, WritableSignal } from '@angular/core';
import { ActivatedRoute } from '@angular/router';

import { AuthSessionService } from '../../core/auth/auth-session.service';
import { UserProfileApi } from '../../core/user-profiles/infrastructure/user-profile.api';
import {
  UserProfileListResponse,
  UserProfileResponse,
  UserProfileSummaryDto,
  UserSavedItemsResponse,
} from '../../core/user-profiles/user-profile.models';

type PublicProfileLoadStatus = 'loading' | 'loaded' | 'error';
type PublicProfileSectionStatus = 'loading' | 'loaded' | 'empty' | 'error';
type FollowActionStatus = 'idle' | 'pending' | 'success' | 'error' | 'gated';

@Injectable()
export class PublicProfileStateService {
  private readonly activatedRoute = inject(ActivatedRoute);
  private readonly userProfileApi = inject(UserProfileApi);
  private readonly authSessionService = inject(AuthSessionService);

  private readonly identityId = this.activatedRoute.snapshot.paramMap.get('identityId') ?? '';
  private readonly loadStatusState = signal<PublicProfileLoadStatus>('loading');
  private readonly followersStatusState = signal<PublicProfileSectionStatus>('loading');
  private readonly followingStatusState = signal<PublicProfileSectionStatus>('loading');
  private readonly savedItemsStatusState = signal<PublicProfileSectionStatus>('loading');
  private readonly followActionStatusState = signal<FollowActionStatus>('idle');
  private readonly profileState = signal<UserProfileResponse | null>(null);
  private readonly followersState = signal<UserProfileSummaryDto[]>([]);
  private readonly followingState = signal<UserProfileSummaryDto[]>([]);
  private readonly savedItemsState = signal<UserSavedItemsResponse | null>(null);
  private readonly isFollowingState = signal(false);
  private readonly savedItemsLoadedForIdentity = signal<string | null>(null);

  readonly loadStatus = this.loadStatusState.asReadonly();
  readonly followersStatus = this.followersStatusState.asReadonly();
  readonly followingStatus = this.followingStatusState.asReadonly();
  readonly savedItemsStatus = this.savedItemsStatusState.asReadonly();
  readonly followActionStatus = this.followActionStatusState.asReadonly();
  readonly profile = this.profileState.asReadonly();
  readonly followers = this.followersState.asReadonly();
  readonly following = this.followingState.asReadonly();
  readonly savedItems = this.savedItemsState.asReadonly();
  readonly isFollowing = this.isFollowingState.asReadonly();
  readonly authState = this.authSessionService.authState;
  readonly isAuthenticated = computed(() => this.authState().loadStatus === 'authenticated');
  readonly signedInIdentityId = computed(() => this.authState().session?.identity.identityId ?? null);
  readonly isOwnProfile = computed(() => this.signedInIdentityId() === this.identityId);
  readonly displayName = computed(() => this.profile()?.displayName?.trim() || 'FoodSocial profile');
  readonly pageTitle = computed(() => this.displayName());
  readonly profileBio = computed(() => this.profile()?.bio?.trim() || 'This profile has not added a bio yet.');
  readonly followerLabel = computed(() => `${this.profile()?.followerCount ?? 0} followers`);
  readonly followingLabel = computed(() => `${this.profile()?.followingCount ?? 0} following`);
  readonly preferredCuisinesText = computed(() => this.formatList(this.profile()?.preferredCuisines, 'No preferred cuisines yet.'));
  readonly dislikedIngredientsText = computed(() => this.formatList(this.profile()?.dislikedIngredients, 'No avoided ingredients yet.'));
  readonly dietaryRestrictionsText = computed(() => this.formatList(this.profile()?.dietaryRestrictions, 'No dietary notes yet.'));
  readonly avatarInitials = computed(() => this.buildInitials(this.displayName()));
  readonly savedPostsLabel = computed(() => `${this.savedItems()?.posts.length ?? 0} saved posts`);
  readonly savedDishesLabel = computed(() => `${this.savedItems()?.dishes.length ?? 0} saved dishes`);
  readonly savedPlacesLabel = computed(() => `${this.savedItems()?.places.length ?? 0} saved places`);

  constructor() {
    this.loadProfile();
    this.loadFollowers();
    this.loadFollowing();
    effect(() => {
      if (!this.isOwnProfile()) {
        return;
      }

      if (this.savedItemsLoadedForIdentity() === this.identityId) {
        return;
      }

      this.loadSavedItems();
    });
  }

  toggleFollow(): void {
    if (!this.isAuthenticated()) {
      this.followActionStatusState.set('gated');
      return;
    }

    if (this.isOwnProfile() || this.followActionStatus() === 'pending') {
      return;
    }

    this.followActionStatusState.set('pending');
    const followRequest = this.isFollowing()
      ? this.userProfileApi.unfollow(this.identityId)
      : this.userProfileApi.follow(this.identityId);

    followRequest.subscribe({
      next: (response) => {
        this.isFollowingState.set(response.following);
        this.followActionStatusState.set('success');
        this.loadProfile();
        this.loadFollowers();
      },
      error: () => {
        this.followActionStatusState.set('error');
      },
    });
  }

  startLogin(): void {
    this.followActionStatusState.set('gated');
    this.authSessionService.startLogin();
  }

  summaryName(profile: UserProfileSummaryDto): string {
    return profile.displayName?.trim() || 'FoodSocial profile';
  }

  private loadProfile(): void {
    if (!this.identityId) {
      this.loadStatusState.set('error');
      return;
    }

    this.loadStatusState.set('loading');
    this.userProfileApi.getProfile(this.identityId).subscribe({
      next: (profile) => {
        this.profileState.set(profile);
        this.loadStatusState.set('loaded');
      },
      error: () => {
        this.profileState.set(null);
        this.loadStatusState.set('error');
      },
    });
  }

  private loadFollowers(): void {
    if (!this.identityId) {
      this.followersStatusState.set('error');
      return;
    }

    this.followersStatusState.set('loading');
    this.userProfileApi.getFollowers(this.identityId).subscribe({
      next: (response) => this.applyProfileList(response, this.followersState, this.followersStatusState),
      error: () => {
        this.followersState.set([]);
        this.followersStatusState.set('error');
      },
    });
  }

  private loadFollowing(): void {
    if (!this.identityId) {
      this.followingStatusState.set('error');
      return;
    }

    this.followingStatusState.set('loading');
    this.userProfileApi.getFollowing(this.identityId).subscribe({
      next: (response) => this.applyProfileList(response, this.followingState, this.followingStatusState),
      error: () => {
        this.followingState.set([]);
        this.followingStatusState.set('error');
      },
    });
  }

  private loadSavedItems(): void {
    if (!this.identityId) {
      this.savedItemsStatusState.set('error');
      return;
    }

    this.savedItemsStatusState.set('loading');
    this.savedItemsLoadedForIdentity.set(this.identityId);
    this.userProfileApi.getSavedItems(this.identityId).subscribe({
      next: (savedItems) => {
        this.savedItemsState.set(savedItems);
        this.savedItemsStatusState.set(this.hasAnySavedItem(savedItems) ? 'loaded' : 'empty');
      },
      error: () => {
        this.savedItemsState.set(null);
        this.savedItemsStatusState.set('error');
      },
    });
  }

  private applyProfileList(
    response: UserProfileListResponse,
    target: WritableSignal<UserProfileSummaryDto[]>,
    status: WritableSignal<PublicProfileSectionStatus>,
  ): void {
    target.set(response.items);
    status.set(response.items.length > 0 ? 'loaded' : 'empty');
  }

  private formatList(values: readonly string[] | undefined, emptyText: string): string {
    if (!values || values.length === 0) {
      return emptyText;
    }

    return values.join(', ');
  }

  private hasAnySavedItem(savedItems: UserSavedItemsResponse): boolean {
    return savedItems.posts.length + savedItems.dishes.length + savedItems.places.length > 0;
  }

  private buildInitials(name: string): string {
    return name
      .split(' ')
      .filter((part) => part.length > 0)
      .slice(0, 2)
      .map((part) => part[0]?.toUpperCase() ?? '')
      .join('');
  }
}

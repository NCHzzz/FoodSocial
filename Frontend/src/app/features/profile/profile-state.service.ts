import { computed, inject, Injectable, signal } from '@angular/core';
import { FormControl, FormGroup, Validators } from '@angular/forms';

import { AuthSessionService } from '../../core/auth/auth-session.service';
import { UserProfileApi } from '../../core/user-profiles/infrastructure/user-profile.api';
import { PostApi } from '../../core/posts/infrastructure/post.api';
import { PostSummaryResponse } from '../../core/posts/post.models';
import {
  UpdateUserProfileRequest,
  UserProfileResponse,
  UserSavedItemsResponse,
} from '../../core/user-profiles/user-profile.models';

type OwnProfileLoadStatus = 'loading' | 'loaded' | 'error';
type OwnProfileSaveStatus = 'idle' | 'pending' | 'success' | 'error';
type OwnSavedItemsStatus = 'loading' | 'loaded' | 'empty' | 'error';
type OwnAuthoredPostsStatus = 'loading' | 'loaded' | 'empty' | 'error';

interface SavedItemView {
  readonly id: string;
  readonly label: string;
  readonly savedAtUtc: string;
}

@Injectable()
export class ProfileStateService {
  private readonly authSessionService = inject(AuthSessionService);
  private readonly userProfileApi = inject(UserProfileApi);
  private readonly postApi = inject(PostApi);

  private readonly profileLoadStatusState = signal<OwnProfileLoadStatus>('loading');
  private readonly saveStatusState = signal<OwnProfileSaveStatus>('idle');
  private readonly savedItemsStatusState = signal<OwnSavedItemsStatus>('loading');
  private readonly authoredPostsStatusState = signal<OwnAuthoredPostsStatus>('loading');
  private readonly profileState = signal<UserProfileResponse | null>(null);
  private readonly savedItemsState = signal<UserSavedItemsResponse | null>(null);
  private readonly authoredPostsState = signal<PostSummaryResponse[]>([]);

  readonly profileLoadStatus = this.profileLoadStatusState.asReadonly();
  readonly saveStatus = this.saveStatusState.asReadonly();
  readonly savedItemsStatus = this.savedItemsStatusState.asReadonly();
  readonly authoredPostsStatus = this.authoredPostsStatusState.asReadonly();
  readonly profile = this.profileState.asReadonly();
  readonly savedItems = this.savedItemsState.asReadonly();
  readonly authoredPosts = this.authoredPostsState.asReadonly();

  readonly profileForm = new FormGroup({
    displayName: new FormControl('', { nonNullable: true, validators: [Validators.maxLength(80)] }),
    avatarUrl: new FormControl('', {
      nonNullable: true,
      validators: [Validators.pattern(/^$|https?:\/\/.+/u)],
    }),
    bio: new FormControl('', { nonNullable: true, validators: [Validators.maxLength(280)] }),
    preferredCuisines: new FormControl('', { nonNullable: true }),
    dislikedIngredients: new FormControl('', { nonNullable: true }),
    dietaryRestrictions: new FormControl('', { nonNullable: true }),
  });

  readonly ownIdentityId = computed(() => this.authSessionService.authState().session?.identity.identityId ?? null);
  readonly savedPosts = computed<SavedItemView[]>(() =>
    (this.savedItems()?.posts ?? []).map((item) => ({
      id: item.postId,
      label: `Post ${item.postId}`,
      savedAtUtc: item.savedAtUtc,
    })),
  );
  readonly savedDishes = computed<SavedItemView[]>(() =>
    (this.savedItems()?.dishes ?? []).map((item) => ({
      id: item.dishId,
      label: `Dish ${item.dishId}`,
      savedAtUtc: item.savedAtUtc,
    })),
  );
  readonly savedPlaces = computed<SavedItemView[]>(() =>
    (this.savedItems()?.places ?? []).map((item) => ({
      id: item.placeId,
      label: `Place ${item.placeId}`,
      savedAtUtc: item.savedAtUtc,
    })),
  );

  constructor() {
    this.loadOwnProfile();
    this.loadSavedItems();
    this.loadAuthoredPosts();
  }

  loadOwnProfile(): void {
    const identityId = this.ownIdentityId();

    if (!identityId) {
      this.profileLoadStatusState.set('error');
      return;
    }

    this.profileLoadStatusState.set('loading');
    this.userProfileApi.getProfile(identityId).subscribe({
      next: (profile) => {
        this.profileState.set(profile);
        this.profileForm.reset({
          displayName: profile.displayName ?? '',
          avatarUrl: profile.avatarUrl ?? '',
          bio: profile.bio ?? '',
          preferredCuisines: profile.preferredCuisines.join(', '),
          dislikedIngredients: profile.dislikedIngredients.join(', '),
          dietaryRestrictions: profile.dietaryRestrictions.join(', '),
        });
        this.saveStatusState.set('idle');
        this.profileLoadStatusState.set('loaded');
      },
      error: () => {
        this.profileState.set(null);
        this.profileLoadStatusState.set('error');
      },
    });
  }

  saveProfile(): void {
    const identityId = this.ownIdentityId();

    if (!identityId || !this.canSubmit()) {
      this.profileForm.markAllAsTouched();
      return;
    }

    this.saveStatusState.set('pending');
    this.userProfileApi.updateProfile(identityId, this.buildUpdateRequest()).subscribe({
      next: (profile) => {
        this.profileState.set(profile);
        this.saveStatusState.set('success');
      },
      error: () => {
        this.saveStatusState.set('error');
      },
    });
  }

  canSubmit(): boolean {
    return this.profileForm.valid && this.saveStatus() !== 'pending';
  }

  displayNamePreview(): string {
    return this.trimToNull(this.profileForm.controls.displayName.value) ?? 'FoodSocial profile';
  }

  bioPreview(): string {
    return this.trimToNull(this.profileForm.controls.bio.value) ?? 'Add a short note about what you like to eat and whose recommendations you trust.';
  }

  avatarInitials(): string {
    return this.buildInitials(this.displayNamePreview());
  }

  loadSavedItems(): void {
    const identityId = this.ownIdentityId();

    if (!identityId) {
      this.savedItemsStatusState.set('error');
      return;
    }

    this.savedItemsStatusState.set('loading');
    this.userProfileApi.getSavedItems(identityId).subscribe({
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

  loadAuthoredPosts(): void {
    const identityId = this.ownIdentityId();

    if (!identityId) {
      this.authoredPostsState.set([]);
      this.authoredPostsStatusState.set('error');
      return;
    }

    this.authoredPostsStatusState.set('loading');
    this.postApi.searchPosts({ authorId: identityId, limit: 12 }).subscribe({
      next: (response) => {
        this.authoredPostsState.set(response.items);
        this.authoredPostsStatusState.set(response.items.length > 0 ? 'loaded' : 'empty');
      },
      error: () => {
        this.authoredPostsState.set([]);
        this.authoredPostsStatusState.set('error');
      },
    });
  }

  private buildUpdateRequest(): UpdateUserProfileRequest {
    return {
      displayName: this.trimToNull(this.profileForm.controls.displayName.value),
      avatarUrl: this.trimToNull(this.profileForm.controls.avatarUrl.value),
      bio: this.trimToNull(this.profileForm.controls.bio.value),
      preferredCuisines: this.parseList(this.profileForm.controls.preferredCuisines.value),
      dislikedIngredients: this.parseList(this.profileForm.controls.dislikedIngredients.value),
      dietaryRestrictions: this.parseList(this.profileForm.controls.dietaryRestrictions.value),
    };
  }

  private parseList(value: string): string[] {
    return value
      .split(',')
      .map((item) => item.trim())
      .filter((item) => item.length > 0);
  }

  private trimToNull(value: string): string | null {
    const trimmedValue = value.trim();

    return trimmedValue.length > 0 ? trimmedValue : null;
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

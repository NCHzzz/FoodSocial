import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { RouterLink } from '@angular/router';

import { AuthSessionService } from '../../core/auth/auth-session.service';
import { UserProfileApi } from '../../core/user-profiles/infrastructure/user-profile.api';
import {
  UpdateUserProfileRequest,
  UserProfileResponse,
  UserSavedItemsResponse,
} from '../../core/user-profiles/user-profile.models';
import { UiButtonDirective } from '../../shared/ui/button/ui-button.directive';
import { UiPageScaffoldComponent } from '../../shared/ui/page-scaffold/ui-page-scaffold.component';
import { UiSurfaceDirective } from '../../shared/ui/surface/ui-surface.directive';

type OwnProfileLoadStatus = 'loading' | 'loaded' | 'error';
type OwnProfileSaveStatus = 'idle' | 'pending' | 'success' | 'error';
type OwnSavedItemsStatus = 'loading' | 'loaded' | 'empty' | 'error';

interface SavedItemView {
  readonly id: string;
  readonly label: string;
  readonly savedAtUtc: string;
}

@Component({
  selector: 'app-profile-page',
  imports: [ReactiveFormsModule, RouterLink, UiButtonDirective, UiPageScaffoldComponent, UiSurfaceDirective],
  template: `
    <app-ui-page-scaffold
      eyebrow="Authenticated profile"
      title="Profile"
      description="Manage the identity and taste signals FoodSocial uses to make food discovery feel personal without hiding saved activity behind a separate dashboard."
    >
      <div class="mt-8 grid gap-4">
        @if (profileLoadStatus() === 'loading') {
          <div appUiSurface="soft" appUiSurfacePadding="comfortable">
            <p class="text-sm font-semibold text-[#141413]">Loading your profile</p>
            <p class="mt-2 text-sm leading-6 text-[#5e5d59]">
              Fetching your current profile through the authenticated BFF session.
            </p>
          </div>
        }

        @if (profileLoadStatus() === 'error') {
          <div appUiSurface="soft" appUiSurfacePadding="comfortable">
            <p class="text-sm font-semibold text-[#141413]">Your profile could not load</p>
            <p class="mt-2 text-sm leading-6 text-[#5e5d59]">
              The edit form stays locked until FoodSocial can confirm your session and profile state.
            </p>
            <button class="mt-4" type="button" appUiButton="secondary" (click)="loadOwnProfile()">
              Retry profile load
            </button>
          </div>
        }

        @if (profile(); as loadedProfile) {
          <section class="grid gap-4 lg:grid-cols-[minmax(0,1fr)_18rem]">
            <form
              appUiSurface="default"
              appUiSurfacePadding="comfortable"
              class="grid gap-5"
              [formGroup]="profileForm"
              (ngSubmit)="saveProfile()"
            >
              <div>
                <p class="text-xs font-semibold uppercase tracking-[0.22em] text-[#c96442]">Edit basics</p>
                <h2 class="mt-2 text-xl font-semibold tracking-tight text-[#141413]">Public identity</h2>
                <p class="mt-2 text-sm leading-6 text-[#5e5d59]">
                  Keep this concise and taste-focused. These fields power the public profile preview.
                </p>
              </div>

              <label class="grid gap-2 text-sm font-semibold text-[#141413]">
                Display name
                <input
                  class="min-h-11 rounded-2xl border border-[#e8e6dc] bg-[#faf9f5] px-4 text-sm font-medium text-[#141413] outline-none transition focus:border-[#d1cfc5] focus:ring-2 focus:ring-[#3898ec]/20"
                  type="text"
                  formControlName="displayName"
                  autocomplete="name"
                />
                @if (profileForm.controls.displayName.invalid && profileForm.controls.displayName.touched) {
                  <span class="text-xs font-semibold text-red-600">Use 80 characters or fewer.</span>
                }
              </label>

              <label class="grid gap-2 text-sm font-semibold text-[#141413]">
                Avatar URL
                <input
                  class="min-h-11 rounded-2xl border border-[#e8e6dc] bg-[#faf9f5] px-4 text-sm font-medium text-[#141413] outline-none transition focus:border-[#d1cfc5] focus:ring-2 focus:ring-[#3898ec]/20"
                  type="url"
                  formControlName="avatarUrl"
                  autocomplete="url"
                />
                @if (profileForm.controls.avatarUrl.invalid && profileForm.controls.avatarUrl.touched) {
                  <span class="text-xs font-semibold text-red-600">Enter a valid http or https URL.</span>
                }
              </label>

              <label class="grid gap-2 text-sm font-semibold text-[#141413]">
                Bio
                <textarea
                  class="min-h-32 rounded-2xl border border-[#e8e6dc] bg-[#faf9f5] px-4 py-3 text-sm font-medium leading-6 text-[#141413] outline-none transition focus:border-[#d1cfc5] focus:ring-2 focus:ring-[#3898ec]/20"
                  formControlName="bio"
                  rows="5"
                ></textarea>
                @if (profileForm.controls.bio.invalid && profileForm.controls.bio.touched) {
                  <span class="text-xs font-semibold text-red-600">Keep the bio under 280 characters.</span>
                }
              </label>

              <div class="grid gap-4 md:grid-cols-3">
                <label class="grid gap-2 text-sm font-semibold text-[#141413]">
                  Preferred cuisines
                  <textarea
                    class="min-h-28 rounded-2xl border border-[#e8e6dc] bg-[#faf9f5] px-4 py-3 text-sm font-medium leading-6 text-[#141413] outline-none transition focus:border-[#d1cfc5] focus:ring-2 focus:ring-[#3898ec]/20"
                    formControlName="preferredCuisines"
                    rows="4"
                  ></textarea>
                  <span class="text-xs font-medium text-[#87867f]">Comma-separated, for example: Vietnamese, street food.</span>
                </label>

                <label class="grid gap-2 text-sm font-semibold text-[#141413]">
                  Avoided ingredients
                  <textarea
                    class="min-h-28 rounded-2xl border border-[#e8e6dc] bg-[#faf9f5] px-4 py-3 text-sm font-medium leading-6 text-[#141413] outline-none transition focus:border-[#d1cfc5] focus:ring-2 focus:ring-[#3898ec]/20"
                    formControlName="dislikedIngredients"
                    rows="4"
                  ></textarea>
                  <span class="text-xs font-medium text-[#87867f]">Ingredients you usually skip.</span>
                </label>

                <label class="grid gap-2 text-sm font-semibold text-[#141413]">
                  Dietary notes
                  <textarea
                    class="min-h-28 rounded-2xl border border-[#e8e6dc] bg-[#faf9f5] px-4 py-3 text-sm font-medium leading-6 text-[#141413] outline-none transition focus:border-[#d1cfc5] focus:ring-2 focus:ring-[#3898ec]/20"
                    formControlName="dietaryRestrictions"
                    rows="4"
                  ></textarea>
                  <span class="text-xs font-medium text-[#87867f]">Allergies, vegetarian, halal, or other useful notes.</span>
                </label>
              </div>

              @if (saveStatus() === 'success') {
                <p class="rounded-2xl border border-emerald-200 bg-emerald-50 px-4 py-3 text-sm font-semibold text-emerald-800">
                  Profile saved. Your public preview now reflects the latest taste signals.
                </p>
              }

              @if (saveStatus() === 'error') {
                <p class="rounded-2xl border border-red-200 bg-red-50 px-4 py-3 text-sm font-semibold text-red-700">
                  Profile update failed. Check the fields and try again.
                </p>
              }

              <div class="flex flex-wrap items-center gap-3">
                <button type="submit" appUiButton="accent" [disabled]="!canSubmit()">
                  @if (saveStatus() === 'pending') {
                    Saving profile
                  } @else {
                    Save profile
                  }
                </button>
                <a appUiButton="secondary" [routerLink]="['/profiles', loadedProfile.identityId]">
                  View public profile
                </a>
              </div>
            </form>

            <aside appUiSurface="soft" appUiSurfacePadding="comfortable" class="h-fit">
              <p class="text-xs font-semibold uppercase tracking-[0.22em] text-[#c96442]">Preview</p>
              <div class="mt-4 flex items-center gap-3">
                <div class="flex size-14 shrink-0 items-center justify-center rounded-full bg-[#e8e6dc] text-lg font-semibold text-[#c96442]">
                  {{ avatarInitials() }}
                </div>
                <div class="min-w-0">
                  <p class="truncate text-base font-semibold text-[#141413]" [textContent]="displayNamePreview()"></p>
                  <p class="text-xs font-medium uppercase tracking-[0.18em] text-[#87867f]" [textContent]="loadedProfile.identityId"></p>
                </div>
              </div>
              <p class="mt-4 text-sm leading-6 text-[#5e5d59]" [textContent]="bioPreview()"></p>
              <dl class="mt-5 grid grid-cols-2 gap-3 text-sm">
                <div>
                  <dt class="font-medium text-[#87867f]">Followers</dt>
                  <dd class="font-semibold text-[#141413]" [textContent]="loadedProfile.followerCount"></dd>
                </div>
                <div>
                  <dt class="font-medium text-[#87867f]">Following</dt>
                  <dd class="font-semibold text-[#141413]" [textContent]="loadedProfile.followingCount"></dd>
                </div>
              </dl>
            </aside>
          </section>
        }

        <section appUiSurface="subtle" appUiSurfacePadding="comfortable">
          <div class="flex flex-wrap items-start justify-between gap-3">
            <div>
              <p class="text-xs font-semibold uppercase tracking-[0.22em] text-[#c96442]">Saved context</p>
              <h2 class="mt-2 text-xl font-semibold tracking-tight text-[#141413]">Saved items</h2>
              <p class="mt-2 max-w-2xl text-sm leading-6 text-[#5e5d59]">
                Existing APIs expose saved item identifiers only, so the MVP shows honest saved state without inventing post, dish, or place details.
              </p>
            </div>
            <button type="button" appUiButton="secondary" (click)="loadSavedItems()">Refresh saved</button>
          </div>

          @if (savedItemsStatus() === 'loading') {
            <p class="mt-5 text-sm font-semibold text-[#5e5d59]">Loading saved items...</p>
          }

          @if (savedItemsStatus() === 'error') {
            <p class="mt-5 rounded-2xl border border-red-200 bg-red-50 px-4 py-3 text-sm font-semibold text-red-700">
              Saved items could not load for this authenticated profile.
            </p>
          }

          @if (savedItemsStatus() === 'empty') {
            <p class="mt-5 rounded-2xl border border-[#e8e6dc] bg-[#faf9f5] px-4 py-3 text-sm font-semibold text-[#5e5d59]">
              No saved posts, dishes, or places yet. Saved food context will appear here when you start saving items.
            </p>
          }

          @if (savedItemsStatus() === 'loaded') {
            <div class="mt-5 grid gap-4 md:grid-cols-3">
              <div>
                <h3 class="text-sm font-semibold text-[#141413]">Posts</h3>
                <div class="mt-3 grid gap-2">
                  @for (item of savedPosts(); track item.id) {
                    <p class="rounded-2xl border border-[#e8e6dc] bg-[#faf9f5] px-4 py-3 text-xs font-semibold text-[#5e5d59]" [textContent]="item.label"></p>
                  } @empty {
                    <p class="text-sm text-[#87867f]">No saved posts.</p>
                  }
                </div>
              </div>
              <div>
                <h3 class="text-sm font-semibold text-[#141413]">Dishes</h3>
                <div class="mt-3 grid gap-2">
                  @for (item of savedDishes(); track item.id) {
                    <p class="rounded-2xl border border-[#e8e6dc] bg-[#faf9f5] px-4 py-3 text-xs font-semibold text-[#5e5d59]" [textContent]="item.label"></p>
                  } @empty {
                    <p class="text-sm text-[#87867f]">No saved dishes.</p>
                  }
                </div>
              </div>
              <div>
                <h3 class="text-sm font-semibold text-[#141413]">Places</h3>
                <div class="mt-3 grid gap-2">
                  @for (item of savedPlaces(); track item.id) {
                    <p class="rounded-2xl border border-[#e8e6dc] bg-[#faf9f5] px-4 py-3 text-xs font-semibold text-[#5e5d59]" [textContent]="item.label"></p>
                  } @empty {
                    <p class="text-sm text-[#87867f]">No saved places.</p>
                  }
                </div>
              </div>
            </div>
          }
        </section>
      </div>
    </app-ui-page-scaffold>
  `,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ProfilePage {
  private readonly authSessionService = inject(AuthSessionService);
  private readonly userProfileApi = inject(UserProfileApi);

  protected readonly profileLoadStatus = signal<OwnProfileLoadStatus>('loading');
  protected readonly saveStatus = signal<OwnProfileSaveStatus>('idle');
  protected readonly savedItemsStatus = signal<OwnSavedItemsStatus>('loading');
  protected readonly profile = signal<UserProfileResponse | null>(null);
  protected readonly savedItems = signal<UserSavedItemsResponse | null>(null);

  protected readonly profileForm = new FormGroup({
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

  protected readonly ownIdentityId = computed(() => this.authSessionService.authState().session?.identity.identityId ?? null);
  protected readonly savedPosts = computed<SavedItemView[]>(() =>
    (this.savedItems()?.posts ?? []).map((item) => ({
      id: item.postId,
      label: `Post ${item.postId}`,
      savedAtUtc: item.savedAtUtc,
    })),
  );
  protected readonly savedDishes = computed<SavedItemView[]>(() =>
    (this.savedItems()?.dishes ?? []).map((item) => ({
      id: item.dishId,
      label: `Dish ${item.dishId}`,
      savedAtUtc: item.savedAtUtc,
    })),
  );
  protected readonly savedPlaces = computed<SavedItemView[]>(() =>
    (this.savedItems()?.places ?? []).map((item) => ({
      id: item.placeId,
      label: `Place ${item.placeId}`,
      savedAtUtc: item.savedAtUtc,
    })),
  );

  constructor() {
    this.loadOwnProfile();
    this.loadSavedItems();
  }

  protected loadOwnProfile(): void {
    const identityId = this.ownIdentityId();

    if (!identityId) {
      this.profileLoadStatus.set('error');
      return;
    }

    this.profileLoadStatus.set('loading');
    this.userProfileApi.getProfile(identityId).subscribe({
      next: (profile) => {
        this.profile.set(profile);
        this.profileForm.reset({
          displayName: profile.displayName ?? '',
          avatarUrl: profile.avatarUrl ?? '',
          bio: profile.bio ?? '',
          preferredCuisines: profile.preferredCuisines.join(', '),
          dislikedIngredients: profile.dislikedIngredients.join(', '),
          dietaryRestrictions: profile.dietaryRestrictions.join(', '),
        });
        this.saveStatus.set('idle');
        this.profileLoadStatus.set('loaded');
      },
      error: () => {
        this.profile.set(null);
        this.profileLoadStatus.set('error');
      },
    });
  }

  protected saveProfile(): void {
    const identityId = this.ownIdentityId();

    if (!identityId || !this.canSubmit()) {
      this.profileForm.markAllAsTouched();
      return;
    }

    this.saveStatus.set('pending');
    this.userProfileApi.updateProfile(identityId, this.buildUpdateRequest()).subscribe({
      next: (profile) => {
        this.profile.set(profile);
        this.saveStatus.set('success');
      },
      error: () => {
        this.saveStatus.set('error');
      },
    });
  }

  protected canSubmit(): boolean {
    return this.profileForm.valid && this.saveStatus() !== 'pending';
  }

  protected displayNamePreview(): string {
    return this.trimToNull(this.profileForm.controls.displayName.value) ?? 'FoodSocial profile';
  }

  protected bioPreview(): string {
    return this.trimToNull(this.profileForm.controls.bio.value) ?? 'Add a short note about what you like to eat and whose recommendations you trust.';
  }

  protected avatarInitials(): string {
    return this.buildInitials(this.displayNamePreview());
  }

  protected loadSavedItems(): void {
    const identityId = this.ownIdentityId();

    if (!identityId) {
      this.savedItemsStatus.set('error');
      return;
    }

    this.savedItemsStatus.set('loading');
    this.userProfileApi.getSavedItems(identityId).subscribe({
      next: (savedItems) => {
        this.savedItems.set(savedItems);
        this.savedItemsStatus.set(this.hasAnySavedItem(savedItems) ? 'loaded' : 'empty');
      },
      error: () => {
        this.savedItems.set(null);
        this.savedItemsStatus.set('error');
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

import { ChangeDetectionStrategy, Component, computed, effect, inject, signal } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';

import { AuthSessionService } from '../../core/auth/auth-session.service';
import { UserProfileApi } from '../../core/user-profiles/infrastructure/user-profile.api';
import {
  UserProfileListResponse,
  UserProfileResponse,
  UserProfileSummaryDto,
  UserSavedItemsResponse,
} from '../../core/user-profiles/user-profile.models';
import { UiButtonDirective } from '../../shared/ui/button/ui-button.directive';
import { UiPageScaffoldComponent } from '../../shared/ui/page-scaffold/ui-page-scaffold.component';
import { UiSurfaceDirective } from '../../shared/ui/surface/ui-surface.directive';

type PublicProfileLoadStatus = 'loading' | 'loaded' | 'error';
type PublicProfileSectionStatus = 'loading' | 'loaded' | 'empty' | 'error';
type FollowActionStatus = 'idle' | 'pending' | 'success' | 'error' | 'gated';

@Component({
  selector: 'app-public-profile-page',
  imports: [RouterLink, UiButtonDirective, UiPageScaffoldComponent, UiSurfaceDirective],
  template: `
    <app-ui-page-scaffold
      eyebrow="Public profile"
      [title]="pageTitle()"
      description="See a person’s taste profile and follow graph. Private saved context stays visible only to the profile owner."
    >
      <div class="mt-8 grid gap-4">
        @if (loadStatus() === 'loading') {
          <div appUiSurface="soft" appUiSurfacePadding="comfortable">
            <p class="text-sm font-semibold text-slate-950">Loading profile</p>
            <p class="mt-2 text-sm leading-6 text-slate-600">
              Fetching this public profile through the FoodSocial BFF.
            </p>
          </div>
        }

        @if (loadStatus() === 'error') {
          <div appUiSurface="soft" appUiSurfacePadding="comfortable">
            <p class="text-sm font-semibold text-slate-950">Profile could not load</p>
            <p class="mt-2 text-sm leading-6 text-slate-600">
              Try again from Profiles. The public profile facade is reachable only through the BFF.
            </p>
            <a class="mt-4" appUiButton="secondary" routerLink="/profiles">
              Try again from Profiles
            </a>
          </div>
        }

        @if (profile(); as loadedProfile) {
          <section class="grid gap-4 lg:grid-cols-[minmax(0,1fr)_18rem] lg:items-start">
            <div appUiSurface="soft" appUiSurfacePadding="comfortable">
              <div class="flex flex-col gap-5 sm:flex-row sm:items-start sm:justify-between">
                <div class="flex min-w-0 items-start gap-4">
                  <div
                    class="flex size-16 shrink-0 items-center justify-center rounded-full bg-orange-100 text-xl font-semibold text-orange-700"
                    [textContent]="avatarInitials()"
                  ></div>
                  <div class="min-w-0">
                    <p class="text-lg font-semibold tracking-tight text-slate-950" [textContent]="displayName()"></p>
                    <p class="mt-1 break-all text-xs font-medium uppercase tracking-[0.2em] text-slate-500" [textContent]="loadedProfile.identityId"></p>
                    <p class="mt-4 max-w-2xl text-sm leading-7 text-slate-600" [textContent]="profileBio()"></p>
                  </div>
                </div>

                @if (isOwnProfile()) {
                  <a appUiButton="secondary" routerLink="/profile">Edit profile</a>
                } @else {
                  @if (isAuthenticated()) {
                    <button type="button" appUiButton="accent" [disabled]="followActionStatus() === 'pending'" (click)="toggleFollow()">
                      @if (followActionStatus() === 'pending') {
                        Updating
                      } @else if (isFollowing()) {
                        Unfollow
                      } @else {
                        Follow
                      }
                    </button>
                  } @else {
                    <button type="button" appUiButton="secondary" (click)="startLogin()">Log in to follow</button>
                  }
                }
              </div>

              @if (followActionStatus() === 'success') {
                <p class="mt-5 rounded-2xl border border-emerald-200 bg-emerald-50 px-4 py-3 text-sm font-semibold text-emerald-800">
                  Follow state updated for this profile.
                </p>
              }

              @if (followActionStatus() === 'error') {
                <p class="mt-5 rounded-2xl border border-red-200 bg-red-50 px-4 py-3 text-sm font-semibold text-red-700">
                  Follow action failed. Please retry when your session is active.
                </p>
              }

              @if (followActionStatus() === 'gated') {
                <p class="mt-5 rounded-2xl border border-amber-200 bg-amber-50 px-4 py-3 text-sm font-semibold text-amber-800">
                  Following is available after login because it changes your account graph.
                </p>
              }
            </div>

            <div appUiSurface="subtle" appUiSurfacePadding="comfortable" class="grid gap-3">
              <p class="text-sm font-semibold text-slate-950" [textContent]="followerLabel()"></p>
              <p class="text-sm font-semibold text-slate-950" [textContent]="followingLabel()"></p>
              <p class="text-xs font-medium leading-5 text-slate-500">
                Counts come from the profile facade; lists below load from the follow graph endpoints.
              </p>
            </div>
          </section>

          <section appUiSurface="default" appUiSurfacePadding="comfortable">
            <p class="text-xs font-semibold uppercase tracking-[0.22em] text-orange-600">Taste profile</p>
            <div class="mt-4 grid gap-4 md:grid-cols-3">
              <div>
                <h2 class="text-sm font-semibold text-slate-950">Preferred cuisines</h2>
                <p class="mt-2 text-sm leading-6 text-slate-600" [textContent]="preferredCuisinesText()"></p>
              </div>
              <div>
                <h2 class="text-sm font-semibold text-slate-950">Avoids</h2>
                <p class="mt-2 text-sm leading-6 text-slate-600" [textContent]="dislikedIngredientsText()"></p>
              </div>
              <div>
                <h2 class="text-sm font-semibold text-slate-950">Dietary notes</h2>
                <p class="mt-2 text-sm leading-6 text-slate-600" [textContent]="dietaryRestrictionsText()"></p>
              </div>
            </div>
          </section>

          <section class="grid gap-4 lg:grid-cols-2">
            <div appUiSurface="subtle" appUiSurfacePadding="comfortable">
              <h2 class="text-base font-semibold text-slate-950">Followers</h2>
              <p class="mt-2 text-sm leading-6 text-slate-600">People who trust this profile’s food perspective.</p>
              @if (followersStatus() === 'loading') {
                <p class="mt-4 text-sm font-semibold text-slate-600">Loading followers...</p>
              }
              @if (followersStatus() === 'error') {
                <p class="mt-4 text-sm font-semibold text-red-700">Followers could not load.</p>
              }
              @if (followersStatus() === 'empty') {
                <p class="mt-4 text-sm font-semibold text-slate-500">No followers yet.</p>
              }
              @if (followersStatus() === 'loaded') {
                <div class="mt-4 grid gap-3">
                  @for (follower of followers(); track follower.identityId) {
                    <a class="rounded-2xl border border-slate-200 bg-white px-4 py-3 transition hover:border-orange-300 focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-slate-950" [routerLink]="['/profiles', follower.identityId]">
                      <span class="block text-sm font-semibold text-slate-950" [textContent]="summaryName(follower)"></span>
                      <span class="mt-1 block break-all text-xs font-medium text-slate-500" [textContent]="follower.identityId"></span>
                    </a>
                  }
                </div>
              }
            </div>

            <div appUiSurface="subtle" appUiSurfacePadding="comfortable">
              <h2 class="text-base font-semibold text-slate-950">Following</h2>
              <p class="mt-2 text-sm leading-6 text-slate-600">Profiles this person follows for trusted food signals.</p>
              @if (followingStatus() === 'loading') {
                <p class="mt-4 text-sm font-semibold text-slate-600">Loading following...</p>
              }
              @if (followingStatus() === 'error') {
                <p class="mt-4 text-sm font-semibold text-red-700">Following could not load.</p>
              }
              @if (followingStatus() === 'empty') {
                <p class="mt-4 text-sm font-semibold text-slate-500">Not following anyone yet.</p>
              }
              @if (followingStatus() === 'loaded') {
                <div class="mt-4 grid gap-3">
                  @for (followedProfile of following(); track followedProfile.identityId) {
                    <a class="rounded-2xl border border-slate-200 bg-white px-4 py-3 transition hover:border-orange-300 focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-slate-950" [routerLink]="['/profiles', followedProfile.identityId]">
                      <span class="block text-sm font-semibold text-slate-950" [textContent]="summaryName(followedProfile)"></span>
                      <span class="mt-1 block break-all text-xs font-medium text-slate-500" [textContent]="followedProfile.identityId"></span>
                    </a>
                  }
                </div>
              }
            </div>
          </section>

          <section appUiSurface="soft" appUiSurfacePadding="comfortable">
            <p class="text-xs font-semibold uppercase tracking-[0.22em] text-orange-600">Saved context</p>
            <h2 class="mt-2 text-base font-semibold text-slate-950">Saved item state</h2>
            <p class="mt-2 max-w-2xl text-sm leading-6 text-slate-600">
              Saved items are owner-scoped because they reveal private account activity. Public viewers see profile-owned taste and follow signals instead.
            </p>

            @if (!isOwnProfile()) {
              <p class="mt-4 rounded-2xl border border-slate-200 bg-white px-4 py-3 text-sm font-semibold text-slate-600">
                Saved posts, dishes, and places are private for this profile.
              </p>
            } @else {
              @if (savedItemsStatus() === 'loading') {
                <p class="mt-4 text-sm font-semibold text-slate-600">Loading saved state...</p>
              }
              @if (savedItemsStatus() === 'error') {
                <p class="mt-4 text-sm font-semibold text-red-700">Saved state could not load for your profile.</p>
              }
              @if (savedItemsStatus() === 'empty') {
                <p class="mt-4 text-sm font-semibold text-slate-500">No saved posts, dishes, or places yet.</p>
              }
              @if (savedItemsStatus() === 'loaded') {
                <div class="mt-4 grid gap-3 text-sm sm:grid-cols-3">
                  <p class="rounded-2xl border border-slate-200 bg-white px-4 py-3 font-semibold text-slate-950" [textContent]="savedPostsLabel()"></p>
                  <p class="rounded-2xl border border-slate-200 bg-white px-4 py-3 font-semibold text-slate-950" [textContent]="savedDishesLabel()"></p>
                  <p class="rounded-2xl border border-slate-200 bg-white px-4 py-3 font-semibold text-slate-950" [textContent]="savedPlacesLabel()"></p>
                </div>
              }
            }
          </section>
        }
      </div>
    </app-ui-page-scaffold>
  `,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class PublicProfilePage {
  private readonly activatedRoute = inject(ActivatedRoute);
  private readonly userProfileApi = inject(UserProfileApi);
  private readonly authSessionService = inject(AuthSessionService);

  private readonly identityId = this.activatedRoute.snapshot.paramMap.get('identityId') ?? '';

  protected readonly loadStatus = signal<PublicProfileLoadStatus>('loading');
  protected readonly followersStatus = signal<PublicProfileSectionStatus>('loading');
  protected readonly followingStatus = signal<PublicProfileSectionStatus>('loading');
  protected readonly savedItemsStatus = signal<PublicProfileSectionStatus>('loading');
  protected readonly followActionStatus = signal<FollowActionStatus>('idle');
  protected readonly profile = signal<UserProfileResponse | null>(null);
  protected readonly followers = signal<UserProfileSummaryDto[]>([]);
  protected readonly following = signal<UserProfileSummaryDto[]>([]);
  protected readonly savedItems = signal<UserSavedItemsResponse | null>(null);
  protected readonly isFollowing = signal(false);
  private readonly savedItemsLoadedForIdentity = signal<string | null>(null);

  protected readonly authState = this.authSessionService.authState;
  protected readonly isAuthenticated = computed(() => this.authState().loadStatus === 'authenticated');
  protected readonly signedInIdentityId = computed(() => this.authState().session?.identity.identityId ?? null);
  protected readonly isOwnProfile = computed(() => this.signedInIdentityId() === this.identityId);
  protected readonly displayName = computed(() => this.profile()?.displayName?.trim() || 'FoodSocial profile');
  protected readonly pageTitle = computed(() => this.displayName());
  protected readonly profileBio = computed(() => this.profile()?.bio?.trim() || 'This profile has not added a bio yet.');
  protected readonly followerLabel = computed(() => `${this.profile()?.followerCount ?? 0} followers`);
  protected readonly followingLabel = computed(() => `${this.profile()?.followingCount ?? 0} following`);
  protected readonly preferredCuisinesText = computed(() => this.formatList(this.profile()?.preferredCuisines, 'No preferred cuisines yet.'));
  protected readonly dislikedIngredientsText = computed(() => this.formatList(this.profile()?.dislikedIngredients, 'No avoided ingredients yet.'));
  protected readonly dietaryRestrictionsText = computed(() => this.formatList(this.profile()?.dietaryRestrictions, 'No dietary notes yet.'));
  protected readonly avatarInitials = computed(() => this.buildInitials(this.displayName()));
  protected readonly savedPostsLabel = computed(() => `${this.savedItems()?.posts.length ?? 0} saved posts`);
  protected readonly savedDishesLabel = computed(() => `${this.savedItems()?.dishes.length ?? 0} saved dishes`);
  protected readonly savedPlacesLabel = computed(() => `${this.savedItems()?.places.length ?? 0} saved places`);

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

  protected toggleFollow(): void {
    if (!this.isAuthenticated()) {
      this.followActionStatus.set('gated');
      return;
    }

    if (this.isOwnProfile() || this.followActionStatus() === 'pending') {
      return;
    }

    this.followActionStatus.set('pending');
    const followRequest = this.isFollowing()
      ? this.userProfileApi.unfollow(this.identityId)
      : this.userProfileApi.follow(this.identityId);

    followRequest.subscribe({
      next: (response) => {
        this.isFollowing.set(response.following);
        this.followActionStatus.set('success');
        this.loadProfile();
        this.loadFollowers();
      },
      error: () => {
        this.followActionStatus.set('error');
      },
    });
  }

  protected startLogin(): void {
    this.followActionStatus.set('gated');
    this.authSessionService.startLogin();
  }

  protected summaryName(profile: UserProfileSummaryDto): string {
    return profile.displayName?.trim() || 'FoodSocial profile';
  }

  private loadProfile(): void {
    if (!this.identityId) {
      this.loadStatus.set('error');
      return;
    }

    this.loadStatus.set('loading');
    this.userProfileApi.getProfile(this.identityId).subscribe({
      next: (profile) => {
        this.profile.set(profile);
        this.loadStatus.set('loaded');
      },
      error: () => {
        this.profile.set(null);
        this.loadStatus.set('error');
      },
    });
  }

  private loadFollowers(): void {
    if (!this.identityId) {
      this.followersStatus.set('error');
      return;
    }

    this.followersStatus.set('loading');
    this.userProfileApi.getFollowers(this.identityId).subscribe({
      next: (response) => this.applyProfileList(response, this.followers, this.followersStatus),
      error: () => {
        this.followers.set([]);
        this.followersStatus.set('error');
      },
    });
  }

  private loadFollowing(): void {
    if (!this.identityId) {
      this.followingStatus.set('error');
      return;
    }

    this.followingStatus.set('loading');
    this.userProfileApi.getFollowing(this.identityId).subscribe({
      next: (response) => this.applyProfileList(response, this.following, this.followingStatus),
      error: () => {
        this.following.set([]);
        this.followingStatus.set('error');
      },
    });
  }

  private loadSavedItems(): void {
    if (!this.identityId) {
      this.savedItemsStatus.set('error');
      return;
    }

    this.savedItemsStatus.set('loading');
    this.savedItemsLoadedForIdentity.set(this.identityId);
    this.userProfileApi.getSavedItems(this.identityId).subscribe({
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

  private applyProfileList(
    response: UserProfileListResponse,
    target: { set: (items: UserProfileSummaryDto[]) => void },
    status: { set: (status: PublicProfileSectionStatus) => void },
  ): void {
    target.set(response.items);
    status.set(response.items.length > 0 ? 'loaded' : 'empty');
  }

  private formatList(values: string[] | undefined, emptyText: string): string {
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

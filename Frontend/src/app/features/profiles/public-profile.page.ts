import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';

import { UserProfileApi } from '../../core/user-profiles/infrastructure/user-profile.api';
import { UserProfileResponse } from '../../core/user-profiles/user-profile.models';
import { UiButtonDirective } from '../../shared/ui/button/ui-button.directive';
import { UiPageScaffoldComponent } from '../../shared/ui/page-scaffold/ui-page-scaffold.component';
import { UiSurfaceDirective } from '../../shared/ui/surface/ui-surface.directive';

type PublicProfileLoadStatus = 'loading' | 'loaded' | 'error';

@Component({
  selector: 'app-public-profile-page',
  imports: [RouterLink, UiButtonDirective, UiPageScaffoldComponent, UiSurfaceDirective],
  template: `
    <app-ui-page-scaffold
      eyebrow="Public profile"
      [title]="pageTitle()"
      description="Profile basics from the BFF user facade. Authored posts and richer saved context can be added when those backend slices are available."
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
          <section class="grid gap-4 sm:grid-cols-[minmax(0,1fr)_auto] sm:items-start">
            <div appUiSurface="soft" appUiSurfacePadding="comfortable">
              <div class="flex items-start gap-4">
                <div
                  class="flex size-16 shrink-0 items-center justify-center rounded-full bg-orange-100 text-xl font-semibold text-orange-700"
                  [textContent]="avatarInitials()"
                ></div>
                <div class="min-w-0">
                  <p class="text-lg font-semibold tracking-tight text-slate-950" [textContent]="displayName()"></p>
                  <p class="mt-1 text-xs font-medium uppercase tracking-[0.2em] text-slate-500" [textContent]="loadedProfile.identityId"></p>
                  <p class="mt-4 max-w-2xl text-sm leading-7 text-slate-600" [textContent]="profileBio()"></p>
                </div>
              </div>
            </div>

            <div appUiSurface="subtle" appUiSurfacePadding="comfortable" class="grid min-w-48 gap-3 sm:text-right">
              <p class="text-sm font-semibold text-slate-950" [textContent]="followerLabel()"></p>
              <p class="text-sm font-semibold text-slate-950" [textContent]="followingLabel()"></p>
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
        }
      </div>
    </app-ui-page-scaffold>
  `,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class PublicProfilePage {
  private readonly activatedRoute = inject(ActivatedRoute);
  private readonly userProfileApi = inject(UserProfileApi);

  private readonly identityId = this.activatedRoute.snapshot.paramMap.get('identityId') ?? '';

  readonly loadStatus = signal<PublicProfileLoadStatus>('loading');
  readonly profile = signal<UserProfileResponse | null>(null);

  readonly displayName = computed(() => this.profile()?.displayName?.trim() || 'FoodSocial profile');
  readonly pageTitle = computed(() => this.displayName());
  readonly profileBio = computed(() => this.profile()?.bio?.trim() || 'This profile has not added a bio yet.');
  readonly followerLabel = computed(() => `${this.profile()?.followerCount ?? 0} followers`);
  readonly followingLabel = computed(() => `${this.profile()?.followingCount ?? 0} following`);
  readonly preferredCuisinesText = computed(() => this.formatList(this.profile()?.preferredCuisines, 'No preferred cuisines yet.'));
  readonly dislikedIngredientsText = computed(() => this.formatList(this.profile()?.dislikedIngredients, 'No avoided ingredients yet.'));
  readonly dietaryRestrictionsText = computed(() => this.formatList(this.profile()?.dietaryRestrictions, 'No dietary notes yet.'));
  readonly avatarInitials = computed(() => this.buildInitials(this.displayName()));

  constructor() {
    this.loadProfile();
  }

  private loadProfile(): void {
    if (!this.identityId) {
      this.loadStatus.set('error');
      return;
    }

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

  private formatList(values: string[] | undefined, emptyText: string): string {
    if (!values || values.length === 0) {
      return emptyText;
    }

    return values.join(', ');
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

import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';

import { AuthSessionService } from '../../core/auth/auth-session.service';
import { UserProfileApi } from '../../core/user-profiles/infrastructure/user-profile.api';
import { UserSavedItemsResponse } from '../../core/user-profiles/user-profile.models';
import { UiButtonDirective } from '../../shared/ui/button/ui-button.directive';
import { UiPageScaffoldComponent } from '../../shared/ui/page-scaffold/ui-page-scaffold.component';
import { UiSurfaceDirective } from '../../shared/ui/surface/ui-surface.directive';

type SavedPageStatus = 'loading' | 'loaded' | 'empty' | 'error';

interface SavedItemView {
  readonly id: string;
  readonly label: string;
  readonly savedAtUtc: string;
  readonly route: readonly unknown[] | null;
}

@Component({
  selector: 'app-saved-page',
  imports: [RouterLink, UiButtonDirective, UiPageScaffoldComponent, UiSurfaceDirective],
  template: `
    <app-ui-page-scaffold
      eyebrow="Saved content"
      title="Saved food"
      description="Revisit saved posts, dishes, and places from one authenticated MVP surface."
    >
      <section class="mt-8 grid gap-4 lg:grid-cols-[minmax(0,1fr)_18rem]">
        <div appUiSurface="soft" appUiSurfacePadding="comfortable">
          <p class="text-xs font-semibold uppercase tracking-[0.22em] text-[#c96442]">Revisit loop</p>
          <h2 class="mt-2 text-xl font-semibold tracking-tight text-[#141413]">Saved items stay separate from profile editing</h2>
          <p class="mt-3 max-w-2xl text-sm leading-6 text-[#5e5d59]">
            FoodSocial MVP needs a direct path back to useful posts, canonical dishes, and places. This page reads the UserProfile saved-item ownership API.
          </p>
          <div class="mt-5 flex flex-wrap gap-3">
            <button type="button" appUiButton="secondary" (click)="loadSavedItems()" [disabled]="status() === 'loading'">Refresh saved</button>
            <a appUiButton="accent" routerLink="/search">Find more food</a>
          </div>
        </div>

        <aside appUiSurface="default" appUiSurfacePadding="comfortable">
          <p class="text-sm font-semibold text-[#141413]">Saved summary</p>
          <dl class="mt-4 grid gap-3 text-sm text-[#5e5d59]">
            <div class="flex items-center justify-between gap-3 rounded-2xl bg-[#f0eee6] px-4 py-3">
              <dt>Posts</dt>
              <dd class="font-semibold text-[#141413]">{{ savedPosts().length }}</dd>
            </div>
            <div class="flex items-center justify-between gap-3 rounded-2xl bg-[#f0eee6] px-4 py-3">
              <dt>Dishes</dt>
              <dd class="font-semibold text-[#141413]">{{ savedDishes().length }}</dd>
            </div>
            <div class="flex items-center justify-between gap-3 rounded-2xl bg-[#f0eee6] px-4 py-3">
              <dt>Places</dt>
              <dd class="font-semibold text-[#141413]">{{ savedPlaces().length }}</dd>
            </div>
          </dl>
        </aside>
      </section>

      <section class="mt-8">
        @if (status() === 'loading') {
          <div appUiSurface="soft" appUiSurfacePadding="comfortable">
            <p class="text-sm font-semibold text-[#141413]">Loading saved content</p>
            <p class="mt-2 text-sm text-[#5e5d59]">Fetching your saved posts, dishes, and places through the BFF.</p>
          </div>
        }

        @if (status() === 'error') {
          <div appUiSurface="soft" appUiSurfacePadding="comfortable">
            <p class="text-sm font-semibold text-[#141413]">Saved content could not load</p>
            <p class="mt-2 text-sm text-[#5e5d59]">Retry when your session and UserProfile facade are reachable.</p>
            <button class="mt-4" type="button" appUiButton="secondary" (click)="loadSavedItems()">Retry</button>
          </div>
        }

        @if (status() === 'empty') {
          <div appUiSurface="soft" appUiSurfacePadding="comfortable">
            <p class="text-sm font-semibold text-[#141413]">No saved food yet</p>
            <p class="mt-2 text-sm text-[#5e5d59]">Save posts, dishes, or places from Discover and Search to build this list.</p>
            <a class="mt-4" appUiButton="accent" routerLink="/discover">Open Discover</a>
          </div>
        }

        @if (status() === 'loaded') {
          <div class="grid gap-5 lg:grid-cols-3">
            <section appUiSurface="default" appUiSurfacePadding="comfortable">
              <p class="text-xs font-semibold uppercase tracking-[0.22em] text-[#c96442]">Saved posts</p>
              <h3 class="mt-2 text-lg font-semibold text-[#141413]">Posts</h3>
              <div class="mt-4 grid gap-3">
                @for (post of savedPosts(); track post.id) {
                  <article class="rounded-2xl border border-[#e8e6dc] bg-[#faf9f5] px-4 py-3">
                    <p class="break-all text-sm font-semibold text-[#141413]" [textContent]="post.label"></p>
                    <p class="mt-1 text-xs font-medium text-[#87867f]">Saved {{ formatDate(post.savedAtUtc) }}</p>
                    @if (post.route) {
                      <a class="mt-3 inline-flex text-sm font-semibold text-[#c96442] hover:text-[#4d4c48]" [routerLink]="post.route">Open post</a>
                    }
                  </article>
                }
              </div>
            </section>

            <section appUiSurface="default" appUiSurfacePadding="comfortable">
              <p class="text-xs font-semibold uppercase tracking-[0.22em] text-[#c96442]">Saved dishes</p>
              <h3 class="mt-2 text-lg font-semibold text-[#141413]">Dishes</h3>
              <div class="mt-4 grid gap-3">
                @for (dish of savedDishes(); track dish.id) {
                  <article class="rounded-2xl border border-[#e8e6dc] bg-[#faf9f5] px-4 py-3">
                    <p class="break-all text-sm font-semibold text-[#141413]" [textContent]="dish.label"></p>
                    <p class="mt-1 text-xs font-medium text-[#87867f]">Saved {{ formatDate(dish.savedAtUtc) }}</p>
                    <a class="mt-3 inline-flex text-sm font-semibold text-[#c96442] hover:text-[#4d4c48]" [routerLink]="['/search']" [queryParams]="{ dishId: dish.id }">Find posts for this dish</a>
                  </article>
                }
              </div>
            </section>

            <section appUiSurface="default" appUiSurfacePadding="comfortable">
              <p class="text-xs font-semibold uppercase tracking-[0.22em] text-[#c96442]">Saved places</p>
              <h3 class="mt-2 text-lg font-semibold text-[#141413]">Places</h3>
              <div class="mt-4 grid gap-3">
                @for (place of savedPlaces(); track place.id) {
                  <article class="rounded-2xl border border-[#e8e6dc] bg-[#faf9f5] px-4 py-3">
                    <p class="break-all text-sm font-semibold text-[#141413]" [textContent]="place.label"></p>
                    <p class="mt-1 text-xs font-medium text-[#87867f]">Saved {{ formatDate(place.savedAtUtc) }}</p>
                    <a class="mt-3 inline-flex text-sm font-semibold text-[#c96442] hover:text-[#4d4c48]" [routerLink]="['/search']" [queryParams]="{ placeId: place.id }">Find posts for this place</a>
                  </article>
                }
              </div>
            </section>
          </div>
        }
      </section>
    </app-ui-page-scaffold>
  `,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class SavedPage {
  private readonly authSessionService = inject(AuthSessionService);
  private readonly userProfileApi = inject(UserProfileApi);

  protected readonly status = signal<SavedPageStatus>('loading');
  private readonly savedItems = signal<UserSavedItemsResponse | null>(null);
  private readonly signedInIdentityId = computed(() => this.authSessionService.authState().session?.identity.identityId ?? null);

  protected readonly savedPosts = computed<SavedItemView[]>(() =>
    (this.savedItems()?.posts ?? []).map((item) => ({
      id: item.postId,
      label: `Post ${item.postId}`,
      savedAtUtc: item.savedAtUtc,
      route: ['/posts', item.postId],
    })),
  );
  protected readonly savedDishes = computed<SavedItemView[]>(() =>
    (this.savedItems()?.dishes ?? []).map((item) => ({
      id: item.dishId,
      label: `Dish ${item.dishId}`,
      savedAtUtc: item.savedAtUtc,
      route: null,
    })),
  );
  protected readonly savedPlaces = computed<SavedItemView[]>(() =>
    (this.savedItems()?.places ?? []).map((item) => ({
      id: item.placeId,
      label: `Place ${item.placeId}`,
      savedAtUtc: item.savedAtUtc,
      route: null,
    })),
  );

  constructor() {
    this.loadSavedItems();
  }

  protected loadSavedItems(): void {
    const identityId = this.signedInIdentityId();

    if (!identityId) {
      this.savedItems.set(null);
      this.status.set('error');
      return;
    }

    this.status.set('loading');
    this.userProfileApi.getSavedItems(identityId).subscribe({
      next: (savedItems) => {
        this.savedItems.set(savedItems);
        this.status.set(this.hasAnySavedItem(savedItems) ? 'loaded' : 'empty');
      },
      error: () => {
        this.savedItems.set(null);
        this.status.set('error');
      },
    });
  }

  protected formatDate(value: string): string {
    return new Date(value).toLocaleDateString();
  }

  private hasAnySavedItem(savedItems: UserSavedItemsResponse): boolean {
    return savedItems.posts.length + savedItems.dishes.length + savedItems.places.length > 0;
  }
}

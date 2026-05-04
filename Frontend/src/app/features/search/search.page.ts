import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { FormControl, ReactiveFormsModule, Validators } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { forkJoin } from 'rxjs';

import { AuthSessionService } from '../../core/auth/auth-session.service';
import { isAuthenticatedSession } from '../../core/auth/auth-session.presentation';
import { BROWSER_WINDOW } from '../../core/auth/browser-window.token';
import { DishApi } from '../../core/dishes/infrastructure/dish.api';
import { DishSummaryResponse } from '../../core/dishes/dish.models';
import { PlaceApi } from '../../core/places/infrastructure/place.api';
import { PlaceSummaryResponse } from '../../core/places/place.models';
import { PostApi } from '../../core/posts/infrastructure/post.api';
import { PostSummaryResponse } from '../../core/posts/post.models';
import { UserProfileApi } from '../../core/user-profiles/infrastructure/user-profile.api';
import { UiButtonDirective } from '../../shared/ui/button/ui-button.directive';
import { UiPageScaffoldComponent } from '../../shared/ui/page-scaffold/ui-page-scaffold.component';
import { UiSurfaceDirective } from '../../shared/ui/surface/ui-surface.directive';
import { PostCardComponent } from '../posts/post-card.component';

type SearchStatus = 'idle' | 'loading' | 'loaded' | 'empty' | 'error';
type SearchActionStatus = 'idle' | 'pending' | 'success' | 'error' | 'gated';

@Component({
  selector: 'app-search-page',
  imports: [ReactiveFormsModule, RouterLink, UiButtonDirective, UiPageScaffoldComponent, UiSurfaceDirective, PostCardComponent],
  template: `
    <app-ui-page-scaffold
      eyebrow="Food discovery search"
      title="Search"
      description="Search posts, dishes, and places through deterministic BFF-backed paths while AI-assisted suggestions are still a later slice."
    >
      <section class="mt-8 grid gap-4 lg:grid-cols-[minmax(0,1fr)_18rem]">
        <form appUiSurface="soft" appUiSurfacePadding="comfortable" (ngSubmit)="runSearch()">
          <p class="text-xs font-semibold uppercase tracking-[0.22em] text-orange-600">Search query</p>
          <h2 class="mt-2 text-xl font-semibold tracking-tight text-slate-950">Food-first search across live catalogs and posts</h2>
          <p class="mt-2 max-w-2xl text-sm leading-6 text-slate-600">
            This page keeps the MVP searchable without pretending AI query assist is ready. Search returns bounded Dish, Place, and Post results from existing BFF APIs.
          </p>
          <div class="mt-5 flex flex-col gap-3 sm:flex-row">
            <label class="min-w-0 flex-1 text-sm font-semibold text-slate-950">
              What are you looking for?
              <input
                class="mt-2 min-h-11 w-full rounded-2xl border border-slate-200 bg-white px-4 text-sm font-medium text-slate-950 outline-none transition focus:border-orange-300 focus:ring-2 focus:ring-orange-100"
                type="search"
                [formControl]="queryControl"
                placeholder="phở, District 1, spicy noodles"
              />
            </label>
            <button class="self-end" type="submit" appUiButton="accent" [disabled]="!canSearch()">Search</button>
          </div>
        </form>

        <aside appUiSurface="default" appUiSurfacePadding="comfortable">
          <p class="text-sm font-semibold text-slate-950">AI assist status</p>
          <p class="mt-3 text-sm leading-6 text-slate-600">
            AI-assisted search and recommendation explanations remain pending. Deterministic results continue to work as the safe fallback path.
          </p>
          <a class="mt-5" appUiButton="secondary" routerLink="/discover">Back to Discover</a>
        </aside>
      </section>

      @if (actionStatus() === 'success') {
        <p class="mt-5 rounded-2xl border border-emerald-200 bg-emerald-50 px-4 py-3 text-sm font-semibold text-emerald-800" [textContent]="actionMessage()"></p>
      }
      @if (actionStatus() === 'error') {
        <p class="mt-5 rounded-2xl border border-red-200 bg-red-50 px-4 py-3 text-sm font-semibold text-red-700" [textContent]="actionMessage()"></p>
      }
      @if (actionStatus() === 'gated') {
        <p class="mt-5 rounded-2xl border border-amber-200 bg-amber-50 px-4 py-3 text-sm font-semibold text-amber-900" [textContent]="actionMessage()"></p>
      }

      <section class="mt-8">
        @if (status() === 'idle') {
          <div appUiSurface="soft" appUiSurfacePadding="comfortable">
            <p class="text-sm font-semibold text-slate-950">Start with a dish, place, or area</p>
            <p class="mt-2 text-sm text-slate-600">Try “phở”, “quán”, “District 1”, or a cuisine tag.</p>
          </div>
        }

        @if (status() === 'loading') {
          <div appUiSurface="soft" appUiSurfacePadding="comfortable">
            <p class="text-sm font-semibold text-slate-950">Searching FoodSocial</p>
            <p class="mt-2 text-sm text-slate-600">Querying Dish, Place, and Post facades in parallel.</p>
          </div>
        }

        @if (status() === 'error') {
          <div appUiSurface="soft" appUiSurfacePadding="comfortable">
            <p class="text-sm font-semibold text-slate-950">Search could not complete</p>
            <p class="mt-2 text-sm text-slate-600">One or more BFF search facades failed. Retry with the same query.</p>
          </div>
        }

        @if (status() === 'empty') {
          <div appUiSurface="soft" appUiSurfacePadding="comfortable">
            <p class="text-sm font-semibold text-slate-950">No results yet</p>
            <p class="mt-2 text-sm text-slate-600">Try a broader dish, restaurant, area, or tag query.</p>
          </div>
        }

        @if (status() === 'loaded') {
          <div class="grid gap-6">
            <section appUiSurface="default" appUiSurfacePadding="comfortable">
              <p class="text-xs font-semibold uppercase tracking-[0.22em] text-orange-600">Dishes</p>
              <div class="mt-4 grid gap-3 md:grid-cols-2 xl:grid-cols-3">
                @for (dish of dishes(); track dish.id) {
                  <article class="rounded-2xl border border-slate-200 bg-slate-50 px-4 py-3">
                    <h3 class="text-base font-semibold text-slate-950" [textContent]="dish.name"></h3>
                    <p class="mt-1 text-sm text-slate-600">
                      <span [textContent]="dish.category ?? 'Category pending'"></span>
                      <span aria-hidden="true"> · </span>
                      <span [textContent]="dish.cuisineType ?? 'Cuisine pending'"></span>
                    </p>
                    <button class="mt-3" type="button" appUiButton="secondary" (click)="saveDish(dish.id)">Save dish</button>
                  </article>
                } @empty {
                  <p class="text-sm font-semibold text-slate-500">No dish matches.</p>
                }
              </div>
            </section>

            <section appUiSurface="default" appUiSurfacePadding="comfortable">
              <p class="text-xs font-semibold uppercase tracking-[0.22em] text-orange-600">Places</p>
              <div class="mt-4 grid gap-3 md:grid-cols-2 xl:grid-cols-3">
                @for (place of places(); track place.id) {
                  <article class="rounded-2xl border border-slate-200 bg-slate-50 px-4 py-3">
                    <h3 class="text-base font-semibold text-slate-950" [textContent]="place.name"></h3>
                    <p class="mt-1 text-sm text-slate-600">
                      <span [textContent]="place.area"></span>
                      <span aria-hidden="true"> · </span>
                      <span [textContent]="place.city"></span>
                    </p>
                    <button class="mt-3" type="button" appUiButton="secondary" (click)="savePlace(place.id)">Save place</button>
                  </article>
                } @empty {
                  <p class="text-sm font-semibold text-slate-500">No place matches.</p>
                }
              </div>
            </section>

            <section>
              <p class="mb-4 text-xs font-semibold uppercase tracking-[0.22em] text-orange-600">Posts</p>
              <div class="grid gap-5 md:grid-cols-2 xl:grid-cols-3">
                @for (post of posts(); track post.id) {
                  <app-post-card
                    [post]="post"
                    (likeRequested)="likePost($event)"
                    (saveRequested)="savePost($event)"
                    (shareRequested)="sharePost($event)"
                  />
                } @empty {
                  <div appUiSurface="soft" appUiSurfacePadding="comfortable">
                    <p class="text-sm font-semibold text-slate-500">No post matches.</p>
                  </div>
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
export class SearchPage {
  private readonly dishApi = inject(DishApi);
  private readonly placeApi = inject(PlaceApi);
  private readonly postApi = inject(PostApi);
  private readonly userProfileApi = inject(UserProfileApi);
  private readonly authSessionService = inject(AuthSessionService);
  private readonly browserWindow = inject(BROWSER_WINDOW);

  protected readonly queryControl = new FormControl('', { nonNullable: true, validators: [Validators.required] });
  protected readonly status = signal<SearchStatus>('idle');
  protected readonly actionStatus = signal<SearchActionStatus>('idle');
  protected readonly actionMessage = signal('');
  protected readonly dishes = signal<DishSummaryResponse[]>([]);
  protected readonly places = signal<PlaceSummaryResponse[]>([]);
  protected readonly posts = signal<PostSummaryResponse[]>([]);
  protected readonly authState = this.authSessionService.authState;
  protected readonly isAuthenticated = computed(() => isAuthenticatedSession(this.authState()));
  protected readonly signedInIdentityId = computed(() => this.authState().session?.identity.identityId ?? null);

  protected canSearch(): boolean {
    return this.queryControl.valid && this.status() !== 'loading';
  }

  protected runSearch(): void {
    if (!this.canSearch()) {
      this.queryControl.markAsTouched();
      return;
    }

    const query = this.queryControl.value.trim();
    this.status.set('loading');
    forkJoin({
      dishes: this.dishApi.searchDishes({ query, limit: 8 }),
      places: this.placeApi.searchPlaces({ query, limit: 8 }),
      posts: this.postApi.searchPosts({ query, limit: 12 }),
    }).subscribe({
      next: (results) => {
        this.dishes.set(results.dishes.items);
        this.places.set(results.places.items);
        this.posts.set(results.posts.items);
        this.status.set(this.hasResults() ? 'loaded' : 'empty');
      },
      error: () => {
        this.dishes.set([]);
        this.places.set([]);
        this.posts.set([]);
        this.status.set('error');
      },
    });
  }

  protected likePost(postId: string): void {
    if (!this.requireAuthenticatedAction('Sign in to like search-result posts.')) return;
    this.actionStatus.set('pending');
    this.postApi.likePost(postId).subscribe({
      next: (response) => {
        this.posts.update((posts) => posts.map((post) => post.id === postId ? { ...post, likeCount: response.likeCount } : post));
        this.showActionSuccess('Post liked.');
      },
      error: () => this.showActionError('Like failed. Retry when Post.Api is available.'),
    });
  }

  protected savePost(postId: string): void {
    const identityId = this.signedInIdentityId();
    if (!identityId || !this.requireAuthenticatedAction('Sign in to save posts.')) return;
    this.actionStatus.set('pending');
    this.userProfileApi.savePost(identityId, postId).subscribe({
      next: () => this.showActionSuccess('Post saved.'),
      error: () => this.showActionError('Save failed. Retry when profile services are available.'),
    });
  }

  protected saveDish(dishId: string): void {
    const identityId = this.signedInIdentityId();
    if (!identityId || !this.requireAuthenticatedAction('Sign in to save dishes.')) return;
    this.actionStatus.set('pending');
    this.userProfileApi.saveDish(identityId, dishId).subscribe({
      next: () => this.showActionSuccess('Dish saved.'),
      error: () => this.showActionError('Dish save failed. Retry when profile services are available.'),
    });
  }

  protected savePlace(placeId: string): void {
    const identityId = this.signedInIdentityId();
    if (!identityId || !this.requireAuthenticatedAction('Sign in to save places.')) return;
    this.actionStatus.set('pending');
    this.userProfileApi.savePlace(identityId, placeId).subscribe({
      next: () => this.showActionSuccess('Place saved.'),
      error: () => this.showActionError('Place save failed. Retry when profile services are available.'),
    });
  }

  protected sharePost(postId: string): void {
    const url = `${this.browserWindow.location.origin}/posts/${encodeURIComponent(postId)}`;
    void this.browserWindow.navigator.clipboard?.writeText(url);
    this.showActionSuccess('Post link copied.');
  }

  private hasResults(): boolean {
    return this.dishes().length + this.places().length + this.posts().length > 0;
  }

  private requireAuthenticatedAction(message: string): boolean {
    if (this.isAuthenticated()) return true;
    this.actionStatus.set('gated');
    this.actionMessage.set(message);
    return false;
  }

  private showActionSuccess(message: string): void {
    this.actionStatus.set('success');
    this.actionMessage.set(message);
  }

  private showActionError(message: string): void {
    this.actionStatus.set('error');
    this.actionMessage.set(message);
  }
}

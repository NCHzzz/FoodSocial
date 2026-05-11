import { computed, inject, Injectable, signal } from '@angular/core';
import { FormControl, Validators } from '@angular/forms';
import { catchError, forkJoin, of } from 'rxjs';

import { AuthSessionService } from '../../core/auth/auth-session.service';
import { isAuthenticatedSession } from '../../core/auth/auth-session.presentation';
import { BROWSER_WINDOW } from '../../core/auth/browser-window.token';
import { DishSummaryResponse } from '../../core/dishes/dish.models';
import { DishApi } from '../../core/dishes/infrastructure/dish.api';
import { PlaceApi } from '../../core/places/infrastructure/place.api';
import { PlaceSummaryResponse } from '../../core/places/place.models';
import { PostApi } from '../../core/posts/infrastructure/post.api';
import { PostSummaryResponse } from '../../core/posts/post.models';
import { SearchAssistApi } from '../../core/search/infrastructure/search-assist.api';
import { SearchAssistResponse } from '../../core/search/search-assist.models';
import { UserProfileApi } from '../../core/user-profiles/infrastructure/user-profile.api';

type SearchStatus = 'idle' | 'loading' | 'loaded' | 'empty' | 'error';
type SearchActionStatus = 'idle' | 'pending' | 'success' | 'error' | 'gated';

@Injectable()
export class SearchStateService {
  private readonly dishApi = inject(DishApi);
  private readonly placeApi = inject(PlaceApi);
  private readonly postApi = inject(PostApi);
  private readonly searchAssistApi = inject(SearchAssistApi);
  private readonly userProfileApi = inject(UserProfileApi);
  private readonly authSessionService = inject(AuthSessionService);
  private readonly browserWindow = inject(BROWSER_WINDOW);

  private readonly statusState = signal<SearchStatus>('idle');
  private readonly actionStatusState = signal<SearchActionStatus>('idle');
  private readonly actionMessageState = signal('');
  private readonly dishesState = signal<DishSummaryResponse[]>([]);
  private readonly placesState = signal<PlaceSummaryResponse[]>([]);
  private readonly postsState = signal<PostSummaryResponse[]>([]);
  private readonly assistState = signal<SearchAssistResponse | null>(null);
  private readonly assistDegradedState = signal(false);

  readonly queryControl = new FormControl('', { nonNullable: true, validators: [Validators.required] });
  readonly status = this.statusState.asReadonly();
  readonly actionStatus = this.actionStatusState.asReadonly();
  readonly actionMessage = this.actionMessageState.asReadonly();
  readonly dishes = this.dishesState.asReadonly();
  readonly places = this.placesState.asReadonly();
  readonly posts = this.postsState.asReadonly();
  readonly assist = this.assistState.asReadonly();
  readonly assistDegraded = this.assistDegradedState.asReadonly();
  readonly authState = this.authSessionService.authState;
  readonly isAuthenticated = computed(() => isAuthenticatedSession(this.authState()));
  readonly signedInIdentityId = computed(() => this.authState().session?.identity.identityId ?? null);

  canSearch(): boolean {
    return this.queryControl.valid && this.status() !== 'loading';
  }

  runSearch(): void {
    if (!this.canSearch()) {
      this.queryControl.markAsTouched();
      return;
    }

    const query = this.queryControl.value.trim();
    this.statusState.set('loading');
    this.assistDegradedState.set(false);
    forkJoin({
      dishes: this.dishApi.searchDishes({ query, limit: 8 }),
      places: this.placeApi.searchPlaces({ query, limit: 8 }),
      posts: this.postApi.searchPosts({ query, limit: 12 }),
      assist: this.searchAssistApi.assist({ query, location: null }).pipe(catchError(() => of(null))),
    }).subscribe({
      next: (results) => {
        this.dishesState.set(results.dishes.items);
        this.placesState.set(results.places.items);
        this.postsState.set(results.posts.items);
        this.assistState.set(results.assist);
        this.assistDegradedState.set(results.assist === null);
        this.statusState.set(this.hasResults() ? 'loaded' : 'empty');
      },
      error: () => {
        this.dishesState.set([]);
        this.placesState.set([]);
        this.postsState.set([]);
        this.assistState.set(null);
        this.statusState.set('error');
      },
    });
  }

  likePost(postId: string): void {
    if (!this.requireAuthenticatedAction('Sign in to like search-result posts.')) return;
    this.actionStatusState.set('pending');
    this.postApi.likePost(postId).subscribe({
      next: (response) => {
        this.postsState.update((posts) => posts.map((post) => post.id === postId ? { ...post, likeCount: response.likeCount } : post));
        this.showActionSuccess('Post liked.');
      },
      error: () => this.showActionError('Like failed. Retry when Post.Api is available.'),
    });
  }

  savePost(postId: string): void {
    const identityId = this.signedInIdentityId();
    if (!identityId || !this.requireAuthenticatedAction('Sign in to save posts.')) return;
    this.actionStatusState.set('pending');
    this.userProfileApi.savePost(identityId, postId).subscribe({
      next: () => this.showActionSuccess('Post saved.'),
      error: () => this.showActionError('Save failed. Retry when profile services are available.'),
    });
  }

  repostPost(postId: string): void {
    const actorId = this.signedInIdentityId();
    if (!actorId || !this.requireAuthenticatedAction('Sign in to repost search-result posts.')) return;
    this.actionStatusState.set('pending');
    this.postApi.repostPost(postId, { actorId, caption: null }).subscribe({
      next: (response) => {
        this.postsState.update((posts) => posts.map((post) => post.id === postId ? { ...post, repostCount: response.repostCount } : post));
        this.showActionSuccess('Post reposted.');
      },
      error: () => this.showActionError('Repost failed. Retry when Post.Api is available.'),
    });
  }

  saveDish(dishId: string): void {
    const identityId = this.signedInIdentityId();
    if (!identityId || !this.requireAuthenticatedAction('Sign in to save dishes.')) return;
    this.actionStatusState.set('pending');
    this.userProfileApi.saveDish(identityId, dishId).subscribe({
      next: () => this.showActionSuccess('Dish saved.'),
      error: () => this.showActionError('Dish save failed. Retry when profile services are available.'),
    });
  }

  savePlace(placeId: string): void {
    const identityId = this.signedInIdentityId();
    if (!identityId || !this.requireAuthenticatedAction('Sign in to save places.')) return;
    this.actionStatusState.set('pending');
    this.userProfileApi.savePlace(identityId, placeId).subscribe({
      next: () => this.showActionSuccess('Place saved.'),
      error: () => this.showActionError('Place save failed. Retry when profile services are available.'),
    });
  }

  sharePost(postId: string): void {
    const url = `${this.browserWindow.location.origin}/posts/${encodeURIComponent(postId)}`;
    void this.browserWindow.navigator.clipboard?.writeText(url);
    this.showActionSuccess('Post link copied.');
  }

  reportPost(postId: string): void {
    if (!this.requireAuthenticatedAction('Sign in to report search-result posts.')) return;
    this.actionStatusState.set('pending');
    this.postApi.reportPost(postId, 'spam_or_low_quality', 'Reported from search results.').subscribe({
      next: () => this.showActionSuccess('Report submitted for manual review.'),
      error: () => this.showActionError('Report failed. Retry when moderation intake is available.'),
    });
  }

  private hasResults(): boolean {
    return this.dishes().length + this.places().length + this.posts().length > 0;
  }

  private requireAuthenticatedAction(message: string): boolean {
    if (this.isAuthenticated()) return true;
    this.actionStatusState.set('gated');
    this.actionMessageState.set(message);
    return false;
  }

  private showActionSuccess(message: string): void {
    this.actionStatusState.set('success');
    this.actionMessageState.set(message);
  }

  private showActionError(message: string): void {
    this.actionStatusState.set('error');
    this.actionMessageState.set(message);
  }
}

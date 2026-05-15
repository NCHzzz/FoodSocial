import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';

import { AuthSessionService } from '../../core/auth/auth-session.service';
import {
  getAuthSessionBadgeLabel,
  getDiscoverSessionAvailabilityMessage,
  isAuthenticatedSession,
} from '../../core/auth/auth-session.presentation';
import { BROWSER_WINDOW } from '../../core/auth/browser-window.token';
import { DishSummaryResponse } from '../../core/dishes/dish.models';
import { PlaceSummaryResponse } from '../../core/places/place.models';
import { PostApi } from '../../core/posts/infrastructure/post.api';
import { PostSummaryResponse } from '../../core/posts/post.models';
import { UserProfileApi } from '../../core/user-profiles/infrastructure/user-profile.api';
import { DishSearchSelectComponent } from '../dishes/dish-search-select.component';
import { PlaceSearchSelectComponent } from '../places/place-search-select.component';
import { PostCardComponent, PostReportSubmission } from '../posts/post-card.component';
import { UiButtonDirective } from '../../shared/ui/button/ui-button.directive';
import { UiSurfaceDirective } from '../../shared/ui/surface/ui-surface.directive';

type DiscoverFeedStatus = 'loading' | 'loaded' | 'empty' | 'error';
type DiscoverActionStatus = 'idle' | 'pending' | 'success' | 'error' | 'gated';

@Component({
  selector: 'app-discover-page',
  imports: [RouterLink, DishSearchSelectComponent, PlaceSearchSelectComponent, PostCardComponent, UiButtonDirective, UiSurfaceDirective],
  templateUrl: './discover.page.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class DiscoverPage {
  private readonly authSessionService = inject(AuthSessionService);
  private readonly postApi = inject(PostApi);
  private readonly userProfileApi = inject(UserProfileApi);
  private readonly browserWindow = inject(BROWSER_WINDOW);

  protected readonly authState = this.authSessionService.authState;
  protected readonly isAuthenticated = computed(() => isAuthenticatedSession(this.authState()));
  protected readonly session = computed(() => this.authState().session);
  protected readonly roles = computed(() => this.session()?.user.roles ?? []);
  protected readonly sessionStatusLabel = computed(() => getAuthSessionBadgeLabel(this.authState()));
  protected readonly sessionAvailabilityMessage = computed(() => getDiscoverSessionAvailabilityMessage(this.authState()));
  protected readonly selectedDish = signal<DishSummaryResponse | null>(null);
  protected readonly selectedPlace = signal<PlaceSummaryResponse | null>(null);
  protected readonly hasCatalogContext = computed(() => this.selectedDish() !== null || this.selectedPlace() !== null);
  protected readonly feedStatus = signal<DiscoverFeedStatus>('loading');
  protected readonly feedPosts = signal<PostSummaryResponse[]>([]);
  protected readonly actionStatus = signal<DiscoverActionStatus>('idle');
  protected readonly actionMessage = signal('');
  protected readonly signedInIdentityId = computed(() => this.session()?.identity.identityId ?? null);

  constructor() {
    this.loadDiscoverFeed();
  }

  protected selectDish(dish: DishSummaryResponse): void {
    this.selectedDish.set(dish);
  }

  protected selectPlace(place: PlaceSummaryResponse): void {
    this.selectedPlace.set(place);
  }

  protected loadDiscoverFeed(): void {
    this.feedStatus.set('loading');
    this.postApi.getDiscoverFeed(12).subscribe({
      next: (response) => {
        this.feedPosts.set(response.items);
        this.feedStatus.set(response.items.length > 0 ? 'loaded' : 'empty');
      },
      error: () => {
        this.feedPosts.set([]);
        this.feedStatus.set('error');
      },
    });
  }

  protected likePost(postId: string): void {
    if (!this.requireAuthenticatedAction('Sign in to like posts from Discover.')) return;
    this.actionStatus.set('pending');
    this.postApi.likePost(postId).subscribe({
      next: (response) => {
        this.feedPosts.update((posts) => posts.map((post) => post.id === postId ? { ...post, likeCount: response.likeCount } : post));
        this.showActionSuccess('Post liked.');
      },
      error: () => this.showActionError('Like failed. Retry when Post.Api is available.'),
    });
  }

  protected savePost(postId: string): void {
    const identityId = this.signedInIdentityId();
    if (!identityId || !this.requireAuthenticatedAction('Sign in to save posts from Discover.')) return;
    this.actionStatus.set('pending');
    this.userProfileApi.savePost(identityId, postId).subscribe({
      next: () => this.showActionSuccess('Post saved.'),
      error: () => this.showActionError('Save failed. Retry when profile services are available.'),
    });
  }

  protected repostPost(postId: string): void {
    const actorId = this.signedInIdentityId();
    if (!actorId || !this.requireAuthenticatedAction('Sign in to repost from Discover.')) return;
    this.actionStatus.set('pending');
    this.postApi.repostPost(postId, { actorId, caption: null }).subscribe({
      next: (response) => {
        this.feedPosts.update((posts) => posts.map((post) => post.id === postId ? { ...post, repostCount: response.repostCount } : post));
        this.showActionSuccess('Post reposted.');
      },
      error: () => this.showActionError('Repost failed. Retry when Post.Api is available.'),
    });
  }

  protected async sharePost(postId: string): Promise<void> {
    const url = `${this.browserWindow.location.origin}/posts/${encodeURIComponent(postId)}`;
    try {
      await this.browserWindow.navigator.clipboard?.writeText(url);
      this.showActionSuccess('Post link copied.');
    } catch {
      this.showActionError('Share failed. Copy the URL from the address bar.');
    }
  }

  protected reportPost(report: PostReportSubmission): void {
    if (!this.requireAuthenticatedAction('Sign in to report posts from Discover.')) return;
    this.actionStatus.set('pending');
    this.postApi.reportPost(report.postId, report.reason, report.description).subscribe({
      next: () => this.showActionSuccess('Report submitted for manual review.'),
      error: () => this.showActionError('Report failed. Retry when moderation intake is available.'),
    });
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

import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';

import { AuthSessionService } from '../../core/auth/auth-session.service';
import { BROWSER_WINDOW } from '../../core/auth/browser-window.token';
import { PostApi } from '../../core/posts/infrastructure/post.api';
import { PostSummaryResponse } from '../../core/posts/post.models';
import { UserProfileApi } from '../../core/user-profiles/infrastructure/user-profile.api';
import { UiButtonDirective } from '../../shared/ui/button/ui-button.directive';
import { UiPageScaffoldComponent } from '../../shared/ui/page-scaffold/ui-page-scaffold.component';
import { UiSurfaceDirective } from '../../shared/ui/surface/ui-surface.directive';
import { PostCardComponent, PostReportSubmission } from '../posts/post-card.component';

type FollowingFeedStatus = 'loading' | 'loaded' | 'empty' | 'error';
type FollowingActionStatus = 'idle' | 'pending' | 'success' | 'error';

@Component({
  selector: 'app-following-page',
  imports: [RouterLink, UiButtonDirective, UiPageScaffoldComponent, UiSurfaceDirective, PostCardComponent],
  template: `
      <app-ui-page-scaffold
        eyebrow="Authenticated trusted feed"
        title="Following"
        description="Keep up with people you trust. This feed now uses the BFF-composed following feed until a dedicated Feed service is justified."
    >
      <section class="mt-8 grid gap-4 lg:grid-cols-[minmax(0,1fr)_18rem]">
        <div appUiSurface="soft" appUiSurfacePadding="comfortable">
          <p class="text-xs font-semibold uppercase tracking-[0.22em] text-[#c96442]">Trusted feed</p>
          <h2 class="mt-2 text-xl font-semibold tracking-tight text-[#141413]">Follow graph first, recommendation theater later</h2>
          <p class="mt-3 max-w-2xl text-sm leading-6 text-[#5e5d59]">
            The current MVP assembles followed-author posts through existing BFF contracts. It keeps follower priority explicit while Feed service extraction remains deferred.
          </p>
          <div class="mt-5 flex flex-wrap gap-3">
            <button type="button" appUiButton="accent" (click)="loadFollowingFeed()">Refresh Following</button>
            <a appUiButton="secondary" routerLink="/profiles">Find profiles</a>
          </div>
        </div>

        <aside appUiSurface="default" appUiSurfacePadding="comfortable">
          <p class="text-sm font-semibold text-[#141413]">Feed source</p>
          <ul class="mt-3 space-y-3 text-sm leading-6 text-[#5e5d59]">
            <li>Reads the BFF-composed following feed.</li>
            <li>Keeps follow-graph fan-out behind the edge boundary.</li>
            <li>Uses a bounded response while a dedicated Feed service is deferred.</li>
          </ul>
        </aside>
      </section>

      @if (actionStatus() === 'success') {
        <p class="mt-5 rounded-2xl border border-emerald-200 bg-emerald-50 px-4 py-3 text-sm font-semibold text-emerald-800" [textContent]="actionMessage()"></p>
      }
      @if (actionStatus() === 'error') {
        <p class="mt-5 rounded-2xl border border-red-200 bg-red-50 px-4 py-3 text-sm font-semibold text-red-700" [textContent]="actionMessage()"></p>
      }

      <section class="mt-8">
        @if (status() === 'loading') {
          <div appUiSurface="soft" appUiSurfacePadding="comfortable">
            <p class="text-sm font-semibold text-[#141413]">Loading Following feed</p>
            <p class="mt-2 text-sm text-[#5e5d59]">Fetching followed profiles and their recent posts through the BFF.</p>
          </div>
        }

        @if (status() === 'error') {
          <div appUiSurface="soft" appUiSurfacePadding="comfortable">
            <p class="text-sm font-semibold text-[#141413]">Following feed could not load</p>
            <p class="mt-2 text-sm text-[#5e5d59]">Your session or BFF following feed path is temporarily unavailable.</p>
            <button class="mt-4" type="button" appUiButton="secondary" (click)="loadFollowingFeed()">Retry</button>
          </div>
        }

        @if (status() === 'empty') {
          <div appUiSurface="soft" appUiSurfacePadding="comfortable">
            <p class="text-sm font-semibold text-[#141413]">No followed posts yet</p>
            <p class="mt-2 text-sm text-[#5e5d59]">Follow profiles or wait for followed authors to publish food posts.</p>
            <a class="mt-4" appUiButton="accent" routerLink="/profiles">Find profiles to follow</a>
          </div>
        }

        @if (status() === 'loaded') {
          <div class="grid gap-5 md:grid-cols-2 xl:grid-cols-3">
            @for (post of posts(); track post.id) {
              <app-post-card
                [post]="post"
                (likeRequested)="likePost($event)"
                (saveRequested)="savePost($event)"
                (repostRequested)="repostPost($event)"
                (shareRequested)="sharePost($event)"
                (reportRequested)="reportPost($event)"
              />
            }
          </div>
        }
      </section>
    </app-ui-page-scaffold>
  `,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class FollowingPage {
  private readonly authSessionService = inject(AuthSessionService);
  private readonly userProfileApi = inject(UserProfileApi);
  private readonly postApi = inject(PostApi);
  private readonly browserWindow = inject(BROWSER_WINDOW);

  protected readonly authState = this.authSessionService.authState;
  protected readonly signedInIdentityId = computed(() => this.authState().session?.identity.identityId ?? null);
  protected readonly status = signal<FollowingFeedStatus>('loading');
  protected readonly actionStatus = signal<FollowingActionStatus>('idle');
  protected readonly actionMessage = signal('');
  protected readonly posts = signal<PostSummaryResponse[]>([]);

  constructor() {
    this.loadFollowingFeed();
  }

  protected loadFollowingFeed(): void {
    const identityId = this.signedInIdentityId();
    if (!identityId) {
      this.posts.set([]);
      this.status.set('error');
      return;
    }

    this.status.set('loading');
    this.postApi.getFollowingFeed(24).subscribe({
      next: (response) => {
        this.posts.set(response.items);
        this.status.set(response.items.length > 0 ? 'loaded' : 'empty');
      },
      error: () => {
        this.posts.set([]);
        this.status.set('error');
      },
    });
  }

  protected likePost(postId: string): void {
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
    if (!identityId) {
      this.showActionError('Your session is required before saving posts.');
      return;
    }

    this.actionStatus.set('pending');
    this.userProfileApi.savePost(identityId, postId).subscribe({
      next: () => this.showActionSuccess('Post saved.'),
      error: () => this.showActionError('Save failed. Retry when profile services are available.'),
    });
  }

  protected repostPost(postId: string): void {
    const actorId = this.signedInIdentityId();
    if (!actorId) {
      this.showActionError('Your session is required before reposting.');
      return;
    }

    this.actionStatus.set('pending');
    this.postApi.repostPost(postId, { actorId, caption: null }).subscribe({
      next: (response) => {
        this.posts.update((posts) => posts.map((post) => post.id === postId ? { ...post, repostCount: response.repostCount } : post));
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
    this.actionStatus.set('pending');
    this.postApi.reportPost(report.postId, report.reason, report.description).subscribe({
      next: () => this.showActionSuccess('Report submitted for manual review.'),
      error: () => this.showActionError('Report failed. Retry when moderation intake is available.'),
    });
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

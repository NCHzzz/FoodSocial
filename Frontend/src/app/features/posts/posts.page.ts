import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { FormControl, ReactiveFormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';

import { AuthSessionService } from '../../core/auth/auth-session.service';
import { isAuthenticatedSession } from '../../core/auth/auth-session.presentation';
import { BROWSER_WINDOW } from '../../core/auth/browser-window.token';
import { PostApi } from '../../core/posts/infrastructure/post.api';
import { PostSummaryResponse } from '../../core/posts/post.models';
import { UserProfileApi } from '../../core/user-profiles/infrastructure/user-profile.api';
import { UiButtonDirective } from '../../shared/ui/button/ui-button.directive';
import { UiPageScaffoldComponent } from '../../shared/ui/page-scaffold/ui-page-scaffold.component';
import { UiSurfaceDirective } from '../../shared/ui/surface/ui-surface.directive';
import { PostCardComponent } from './post-card.component';

type PostListStatus = 'loading' | 'loaded' | 'empty' | 'error';
type PostListActionStatus = 'idle' | 'pending' | 'success' | 'error' | 'gated';

@Component({
  selector: 'app-posts-page',
  imports: [ReactiveFormsModule, RouterLink, UiButtonDirective, UiPageScaffoldComponent, UiSurfaceDirective, PostCardComponent],
  template: `
    <app-ui-page-scaffold
      eyebrow="Public food posts"
      title="Posts"
      description="Browse real public food posts from the BFF, then open a post for comments, reports, and deeper context."
    >
      <section class="mt-8 grid gap-4 lg:grid-cols-[minmax(0,1fr)_18rem]">
        <form appUiSurface="soft" appUiSurfacePadding="comfortable" (ngSubmit)="searchPosts()">
          <p class="text-xs font-semibold uppercase tracking-[0.22em] text-orange-600">Post search</p>
          <h2 class="mt-2 text-xl font-semibold tracking-tight text-slate-950">Search captions, tags, and location text</h2>
          <p class="mt-3 max-w-2xl text-sm leading-6 text-slate-600">
            This is the deterministic post search path. Dish and Place catalog search stay available on Discover while richer AI search is still pending.
          </p>
          <div class="mt-5 flex flex-col gap-3 sm:flex-row">
            <label class="min-w-0 flex-1 text-sm font-semibold text-slate-950">
              Search posts
              <input
                class="mt-2 min-h-11 w-full rounded-2xl border border-slate-200 bg-white px-4 text-sm font-medium text-slate-950 outline-none transition focus:border-orange-300 focus:ring-2 focus:ring-orange-100"
                type="search"
                [formControl]="queryControl"
                placeholder="Try phở, spicy, District 1"
              />
            </label>
            <button class="self-end" type="submit" appUiButton="accent" [disabled]="status() === 'loading'">Search</button>
          </div>
        </form>

        <aside appUiSurface="default" appUiSurfacePadding="comfortable">
          <p class="text-sm font-semibold text-slate-950">MVP honesty</p>
          <ul class="mt-3 space-y-3 text-sm leading-6 text-slate-600">
            <li>Cards use real Post.Api summaries.</li>
            <li>Save, like, comment, and report stay gated for signed-in users.</li>
            <li>AI search and repost remain later slices.</li>
          </ul>
          <a class="mt-5" appUiButton="secondary" routerLink="/post-create">Create a post</a>
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
        @if (status() === 'loading') {
          <div appUiSurface="soft" appUiSurfacePadding="comfortable">
            <p class="text-sm font-semibold text-slate-950">Loading posts</p>
            <p class="mt-2 text-sm text-slate-600">Fetching the current public post list through the BFF.</p>
          </div>
        }

        @if (status() === 'error') {
          <div appUiSurface="soft" appUiSurfacePadding="comfortable">
            <p class="text-sm font-semibold text-slate-950">Posts could not load</p>
            <p class="mt-2 text-sm text-slate-600">Post.Api or the BFF post facade is unavailable. Retry keeps the failure visible.</p>
            <button class="mt-4" type="button" appUiButton="secondary" (click)="loadPosts()">Retry</button>
          </div>
        }

        @if (status() === 'empty') {
          <div appUiSurface="soft" appUiSurfacePadding="comfortable">
            <p class="text-sm font-semibold text-slate-950">No posts yet</p>
            <p class="mt-2 text-sm text-slate-600">Create the first food-first post with a dish, caption, and primary media URL.</p>
            <a class="mt-4" appUiButton="accent" routerLink="/post-create">Create first post</a>
          </div>
        }

        @if (status() === 'loaded') {
          <div class="grid gap-5 md:grid-cols-2 xl:grid-cols-3">
            @for (post of posts(); track post.id) {
              <app-post-card
                [post]="post"
                (likeRequested)="likePost($event)"
                (saveRequested)="savePost($event)"
                (shareRequested)="sharePost($event)"
              />
            }
          </div>
        }
      </section>
    </app-ui-page-scaffold>
  `,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class PostsPage {
  private readonly postApi = inject(PostApi);
  private readonly userProfileApi = inject(UserProfileApi);
  private readonly authSessionService = inject(AuthSessionService);
  private readonly browserWindow = inject(BROWSER_WINDOW);

  protected readonly queryControl = new FormControl('', { nonNullable: true });
  protected readonly status = signal<PostListStatus>('loading');
  protected readonly actionStatus = signal<PostListActionStatus>('idle');
  protected readonly actionMessage = signal('');
  protected readonly posts = signal<PostSummaryResponse[]>([]);
  protected readonly authState = this.authSessionService.authState;
  protected readonly signedInIdentityId = computed(() => this.authState().session?.identity.identityId ?? null);

  constructor() {
    this.loadPosts();
  }

  protected loadPosts(): void {
    this.status.set('loading');
    this.postApi.searchPosts({ limit: 24 }).subscribe({
      next: (response) => this.setPosts(response.items),
      error: () => {
        this.posts.set([]);
        this.status.set('error');
      },
    });
  }

  protected searchPosts(): void {
    this.status.set('loading');
    this.postApi.searchPosts({ query: this.queryControl.value, limit: 24 }).subscribe({
      next: (response) => this.setPosts(response.items),
      error: () => {
        this.posts.set([]);
        this.status.set('error');
      },
    });
  }

  protected likePost(postId: string): void {
    if (!this.requireAuthenticatedAction('Sign in to like posts.')) return;
    this.actionStatus.set('pending');
    this.postApi.likePost(postId).subscribe({
      next: (response) => {
        this.posts.update((posts) => posts.map((post) => post.id === postId ? { ...post, likeCount: response.likeCount } : post));
        this.showActionSuccess('Post liked.');
      },
      error: () => this.showActionError('Like failed. Retry when the post service is available.'),
    });
  }

  protected savePost(postId: string): void {
    const identityId = this.signedInIdentityId();
    if (!identityId || !this.requireAuthenticatedAction('Sign in to save posts.')) return;
    this.actionStatus.set('pending');
    this.userProfileApi.savePost(identityId, postId).subscribe({
      next: () => this.showActionSuccess('Post saved to your profile.'),
      error: () => this.showActionError('Save failed. Retry when your profile session is active.'),
    });
  }

  protected sharePost(postId: string): void {
    const url = `${this.browserWindow.location.origin}/posts/${encodeURIComponent(postId)}`;
    void this.browserWindow.navigator.clipboard?.writeText(url);
    this.showActionSuccess('Post link copied for sharing.');
  }

  private setPosts(posts: PostSummaryResponse[]): void {
    this.posts.set(posts);
    this.status.set(posts.length > 0 ? 'loaded' : 'empty');
  }

  private requireAuthenticatedAction(message: string): boolean {
    if (isAuthenticatedSession(this.authState())) return true;
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

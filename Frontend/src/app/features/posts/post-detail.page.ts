import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { FormControl, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, RouterLink } from '@angular/router';

import { AuthSessionService } from '../../core/auth/auth-session.service';
import { isAuthenticatedSession } from '../../core/auth/auth-session.presentation';
import { BROWSER_WINDOW } from '../../core/auth/browser-window.token';
import { PostApi } from '../../core/posts/infrastructure/post.api';
import { PostDetailResponse } from '../../core/posts/post.models';
import { UserProfileApi } from '../../core/user-profiles/infrastructure/user-profile.api';
import { UiButtonDirective } from '../../shared/ui/button/ui-button.directive';
import { UiPageScaffoldComponent } from '../../shared/ui/page-scaffold/ui-page-scaffold.component';
import { UiSurfaceDirective } from '../../shared/ui/surface/ui-surface.directive';

type PostDetailStatus = 'loading' | 'loaded' | 'error';
type PostDetailActionStatus = 'idle' | 'pending' | 'success' | 'error' | 'gated';

@Component({
  selector: 'app-post-detail-page',
  imports: [ReactiveFormsModule, RouterLink, UiButtonDirective, UiPageScaffoldComponent, UiSurfaceDirective],
  template: `
    <app-ui-page-scaffold
      eyebrow="Post detail"
      [title]="pageTitle()"
      description="Open a real post, then use authenticated MVP actions for likes, saves, comments, and manual-review-first reporting."
    >
      <div class="mt-8 grid gap-5">
        @if (status() === 'loading') {
          <div appUiSurface="soft" appUiSurfacePadding="comfortable">
            <p class="text-sm font-semibold text-[#141413]">Loading post</p>
            <p class="mt-2 text-sm text-[#5e5d59]">Fetching the post detail through the BFF.</p>
          </div>
        }

        @if (status() === 'error') {
          <div appUiSurface="soft" appUiSurfacePadding="comfortable">
            <p class="text-sm font-semibold text-[#141413]">Post could not load</p>
            <p class="mt-2 text-sm text-[#5e5d59]">The post may be hidden, deleted, missing, or temporarily unavailable.</p>
            <div class="mt-4 flex flex-wrap gap-3">
              <button type="button" appUiButton="secondary" (click)="loadPost()">Retry</button>
              <a appUiButton="secondary" routerLink="/posts">Back to posts</a>
            </div>
          </div>
        }

        @if (post(); as loadedPost) {
          <article class="grid gap-5 lg:grid-cols-[minmax(0,1fr)_20rem] lg:items-start">
            <section appUiSurface="default" appUiSurfacePadding="comfortable" class="overflow-hidden">
              @if (primaryMediaUrl(); as mediaUrl) {
                <img class="aspect-[4/3] w-full rounded-[1.5rem] object-cover" [src]="mediaUrl" [alt]="mediaAlt()" />
              } @else {
                <div class="flex aspect-[4/3] items-center justify-center rounded-[1.5rem] border border-dashed border-[#e8e6dc] bg-[#f0eee6] text-sm font-semibold text-[#4d4c48]">
                  Media pending
                </div>
              }

              <div class="mt-5">
                <p class="text-xs font-semibold uppercase tracking-[0.22em] text-[#c96442]">Dish identity</p>
                <h2 class="mt-2 text-2xl font-semibold tracking-tight text-[#141413]">{{ dishLabel() }}</h2>
                <p class="mt-4 text-base leading-7 text-[#4d4c48]" [textContent]="loadedPost.caption"></p>
              </div>

              <div class="mt-5 flex flex-wrap gap-2">
                @for (tag of loadedPost.tags; track tag) {
                  <span class="rounded-full bg-[#f0eee6] px-3 py-1 text-xs font-semibold text-[#c96442]">#{{ tag }}</span>
                }
                @if (loadedPost.priceRange) {
                  <span class="rounded-full bg-[#e8e6dc] px-3 py-1 text-xs font-semibold text-[#4d4c48]" [textContent]="loadedPost.priceRange"></span>
                }
                @if (loadedPost.rating) {
                  <span class="rounded-full bg-emerald-50 px-3 py-1 text-xs font-semibold text-emerald-700">{{ loadedPost.rating }}/5</span>
                }
              </div>

              @if (loadedPost.locationText) {
                <p class="mt-4 rounded-2xl border border-[#e8e6dc] bg-[#faf9f5] px-4 py-3 text-sm font-semibold text-[#4d4c48]" [textContent]="loadedPost.locationText"></p>
              }
            </section>

            <aside appUiSurface="soft" appUiSurfacePadding="comfortable" class="grid gap-4">
              <div>
                <p class="text-sm font-semibold text-[#141413]">Post actions</p>
                <p class="mt-2 text-sm leading-6 text-[#5e5d59]">
                  {{ loadedPost.likeCount }} likes · {{ loadedPost.repostCount }} reposts · {{ loadedPost.commentCount }} comments · {{ createdDateLabel() }}
                </p>
              </div>
              <div class="grid gap-2">
                <button type="button" appUiButton="accent" (click)="likePost()">Like post</button>
                <button type="button" appUiButton="secondary" (click)="savePost()">Save post</button>
                <button type="button" appUiButton="secondary" (click)="repostPost()">Repost</button>
                <button type="button" appUiButton="secondary" (click)="sharePost()">Copy share link</button>
                <button type="button" appUiButton="secondary" (click)="reportPost()">Report for review</button>
              </div>
              <a appUiButton="secondary" routerLink="/posts">Back to posts</a>
            </aside>
          </article>

          @if (actionStatus() === 'success') {
            <p class="rounded-2xl border border-emerald-200 bg-emerald-50 px-4 py-3 text-sm font-semibold text-emerald-800" [textContent]="actionMessage()"></p>
          }
          @if (actionStatus() === 'error') {
            <p class="rounded-2xl border border-red-200 bg-red-50 px-4 py-3 text-sm font-semibold text-red-700" [textContent]="actionMessage()"></p>
          }
          @if (actionStatus() === 'gated') {
            <p class="rounded-2xl border border-amber-200 bg-amber-50 px-4 py-3 text-sm font-semibold text-amber-900" [textContent]="actionMessage()"></p>
          }

          <section appUiSurface="default" appUiSurfacePadding="comfortable">
            <div class="flex flex-wrap items-start justify-between gap-3">
              <div>
                <p class="text-xs font-semibold uppercase tracking-[0.22em] text-[#c96442]">Conversation</p>
                <h2 class="mt-2 text-xl font-semibold tracking-tight text-[#141413]">Comments</h2>
              </div>
              @if (!isAuthenticated()) {
                <button type="button" appUiButton="secondary" (click)="startLogin()">Log in to comment</button>
              }
            </div>

            @if (loadedPost.comments.length === 0) {
              <p class="mt-5 rounded-2xl border border-[#e8e6dc] bg-[#faf9f5] px-4 py-3 text-sm font-semibold text-[#5e5d59]">No comments yet.</p>
            } @else {
              <div class="mt-5 grid gap-3">
                @for (comment of loadedPost.comments; track comment.id) {
                  <article class="rounded-2xl border border-[#e8e6dc] bg-[#faf9f5] px-4 py-3">
                    <p class="break-all text-xs font-semibold uppercase tracking-[0.18em] text-[#87867f]" [textContent]="comment.authorId"></p>
                    <p class="mt-2 text-sm leading-6 text-[#4d4c48]" [textContent]="comment.body"></p>
                  </article>
                }
              </div>
            }

            <form class="mt-5 grid gap-3" (ngSubmit)="createComment()">
              <label class="grid gap-2 text-sm font-semibold text-[#141413]">
                Add comment
                <textarea
                  class="min-h-28 rounded-2xl border border-[#e8e6dc] bg-[#faf9f5] px-4 py-3 text-sm font-medium leading-6 text-[#141413] outline-none transition focus:border-[#d1cfc5] focus:ring-2 focus:ring-[#3898ec]/20"
                  [formControl]="commentControl"
                  rows="4"
                  placeholder="Add a useful food note or question."
                ></textarea>
              </label>
              <button class="justify-self-start" type="submit" appUiButton="accent" [disabled]="!canSubmitComment()">Post comment</button>
            </form>
          </section>
        }
      </div>
    </app-ui-page-scaffold>
  `,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class PostDetailPage {
  private readonly activatedRoute = inject(ActivatedRoute);
  private readonly postApi = inject(PostApi);
  private readonly userProfileApi = inject(UserProfileApi);
  private readonly authSessionService = inject(AuthSessionService);
  private readonly browserWindow = inject(BROWSER_WINDOW);

  private readonly postId = this.activatedRoute.snapshot.paramMap.get('postId') ?? '';

  protected readonly status = signal<PostDetailStatus>('loading');
  protected readonly actionStatus = signal<PostDetailActionStatus>('idle');
  protected readonly actionMessage = signal('');
  protected readonly post = signal<PostDetailResponse | null>(null);
  protected readonly commentControl = new FormControl('', { nonNullable: true, validators: [Validators.required, Validators.maxLength(500)] });
  protected readonly authState = this.authSessionService.authState;
  protected readonly isAuthenticated = computed(() => isAuthenticatedSession(this.authState()));
  protected readonly signedInIdentityId = computed(() => this.authState().session?.identity.identityId ?? null);
  protected readonly pageTitle = computed(() => this.post()?.caption.slice(0, 48) || 'Post');
  protected readonly primaryMediaUrl = computed(() => this.post()?.media.find((media) => media.isPrimary)?.url ?? null);
  protected readonly dishLabel = computed(() => `Dish ${this.post()?.dishId ?? ''}`.trim());
  protected readonly mediaAlt = computed(() => `Food photo for ${this.dishLabel()}`);
  protected readonly createdDateLabel = computed(() => {
    const createdAtUtc = this.post()?.createdAtUtc;
    return createdAtUtc ? new Date(createdAtUtc).toLocaleDateString() : '';
  });

  constructor() {
    this.loadPost();
  }

  protected loadPost(): void {
    if (!this.postId) {
      this.status.set('error');
      return;
    }

    this.status.set('loading');
    this.postApi.getPost(this.postId).subscribe({
      next: (post) => {
        this.post.set(post);
        this.status.set('loaded');
      },
      error: () => {
        this.post.set(null);
        this.status.set('error');
      },
    });
  }

  protected likePost(): void {
    if (!this.requireAuthenticatedAction('Sign in to like this post.')) return;
    this.actionStatus.set('pending');
    this.postApi.likePost(this.postId).subscribe({
      next: (response) => {
        this.post.update((post) => post ? { ...post, likeCount: response.likeCount } : post);
        this.showActionSuccess('Post liked.');
      },
      error: () => this.showActionError('Like failed. Retry when the post service is available.'),
    });
  }

  protected savePost(): void {
    const identityId = this.signedInIdentityId();
    if (!identityId || !this.requireAuthenticatedAction('Sign in to save this post.')) return;
    this.actionStatus.set('pending');
    this.userProfileApi.savePost(identityId, this.postId).subscribe({
      next: () => this.showActionSuccess('Post saved to your profile.'),
      error: () => this.showActionError('Save failed. Retry when your profile session is active.'),
    });
  }

  protected repostPost(): void {
    const actorId = this.signedInIdentityId();
    if (!actorId || !this.requireAuthenticatedAction('Sign in to repost this post.')) return;
    this.actionStatus.set('pending');
    this.postApi.repostPost(this.postId, { actorId, caption: null }).subscribe({
      next: (response) => {
        this.post.update((post) => post ? { ...post, repostCount: response.repostCount } : post);
        this.showActionSuccess('Post reposted.');
      },
      error: () => this.showActionError('Repost failed. Retry when the post service is available.'),
    });
  }

  protected sharePost(): void {
    const url = `${this.browserWindow.location.origin}/posts/${encodeURIComponent(this.postId)}`;
    void this.browserWindow.navigator.clipboard?.writeText(url);
    this.showActionSuccess('Post link copied for sharing.');
  }

  protected reportPost(): void {
    if (!this.requireAuthenticatedAction('Sign in to report this post for manual review.')) return;
    this.actionStatus.set('pending');
    this.postApi.reportPost(this.postId, 'spam_or_low_quality', 'Reported from post detail page.').subscribe({
      next: () => this.showActionSuccess('Report submitted for manual review.'),
      error: () => this.showActionError('Report failed. Retry when moderation intake is available.'),
    });
  }

  protected createComment(): void {
    if (!this.requireAuthenticatedAction('Sign in to comment on this post.')) return;
    if (!this.canSubmitComment()) {
      this.commentControl.markAsTouched();
      return;
    }

    this.actionStatus.set('pending');
    this.postApi.createComment(this.postId, this.commentControl.value).subscribe({
      next: () => {
        this.commentControl.reset('');
        this.showActionSuccess('Comment posted.');
        this.loadPost();
      },
      error: () => this.showActionError('Comment failed. Keep it under 500 characters and retry.'),
    });
  }

  protected canSubmitComment(): boolean {
    return this.commentControl.valid && this.actionStatus() !== 'pending';
  }

  protected startLogin(): void {
    this.authSessionService.startLogin();
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

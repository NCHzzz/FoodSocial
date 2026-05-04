import { ChangeDetectionStrategy, Component, computed, input, output } from '@angular/core';
import { RouterLink } from '@angular/router';

import { PostSummaryResponse } from '../../core/posts/post.models';
import { UiButtonDirective } from '../../shared/ui/button/ui-button.directive';
import { UiSurfaceDirective } from '../../shared/ui/surface/ui-surface.directive';

@Component({
  selector: 'app-post-card',
  imports: [RouterLink, UiButtonDirective, UiSurfaceDirective],
  template: `
    <article appUiSurface="default" appUiSurfacePadding="comfortable" class="grid gap-4 overflow-hidden">
      @if (primaryMediaUrl(); as mediaUrl) {
        <a class="block overflow-hidden rounded-[1.5rem] bg-slate-100" [routerLink]="['/posts', post().id]">
          <img class="aspect-[4/3] w-full object-cover" [src]="mediaUrl" [alt]="mediaAlt()" />
        </a>
      } @else {
        <a
          class="flex aspect-[4/3] items-center justify-center rounded-[1.5rem] border border-dashed border-orange-200 bg-orange-50 text-sm font-semibold text-orange-800"
          [routerLink]="['/posts', post().id]"
        >
          Media pending
        </a>
      }

      <div class="grid gap-3">
        <div>
          <p class="text-xs font-semibold uppercase tracking-[0.22em] text-orange-600">Dish-first post</p>
          <a class="mt-1 block text-lg font-semibold tracking-tight text-slate-950 hover:text-orange-700" [routerLink]="['/posts', post().id]">
            {{ dishLabel() }}
          </a>
          <p class="mt-2 line-clamp-3 text-sm leading-6 text-slate-600" [textContent]="post().caption"></p>
        </div>

        <div class="flex flex-wrap gap-2">
          @for (tag of visibleTags(); track tag) {
            <span class="rounded-full bg-orange-50 px-3 py-1 text-xs font-semibold text-orange-700">#{{ tag }}</span>
          }
          @if (post().priceRange) {
            <span class="rounded-full bg-slate-100 px-3 py-1 text-xs font-semibold text-slate-700" [textContent]="post().priceRange"></span>
          }
          @if (post().rating) {
            <span class="rounded-full bg-emerald-50 px-3 py-1 text-xs font-semibold text-emerald-700">{{ post().rating }}/5</span>
          }
        </div>

        <div class="flex flex-wrap items-center gap-2 text-xs font-semibold text-slate-500">
          <span>{{ post().likeCount }} likes</span>
          <span aria-hidden="true">·</span>
          <span>{{ post().commentCount }} comments</span>
          <span aria-hidden="true">·</span>
          <span [textContent]="createdDateLabel()"></span>
        </div>

        <div class="flex flex-wrap gap-2">
          <button type="button" appUiButton="secondary" (click)="likeRequested.emit(post().id)">Like</button>
          <button type="button" appUiButton="secondary" (click)="saveRequested.emit(post().id)">Save</button>
          <button type="button" appUiButton="secondary" (click)="shareRequested.emit(post().id)">Share</button>
          <a appUiButton="accent" [routerLink]="['/posts', post().id]">Open</a>
        </div>
      </div>
    </article>
  `,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class PostCardComponent {
  readonly post = input.required<PostSummaryResponse>();
  readonly likeRequested = output<string>();
  readonly saveRequested = output<string>();
  readonly shareRequested = output<string>();

  protected readonly primaryMediaUrl = computed(() => this.post().primaryMediaUrl?.trim() || null);
  protected readonly visibleTags = computed(() => this.post().tags.slice(0, 4));
  protected readonly dishLabel = computed(() => `Dish ${this.post().dishId}`);
  protected readonly mediaAlt = computed(() => `Food photo for ${this.dishLabel()}`);
  protected readonly createdDateLabel = computed(() => new Date(this.post().createdAtUtc).toLocaleDateString());
}

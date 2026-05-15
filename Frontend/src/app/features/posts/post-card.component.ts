import { ChangeDetectionStrategy, Component, computed, effect, inject, input, output, signal } from '@angular/core';
import { FormControl, ReactiveFormsModule, Validators } from '@angular/forms';
import { RouterLink } from '@angular/router';

import { CatalogLabelService } from '../../core/catalog/catalog-label.service';
import { PostSummaryResponse } from '../../core/posts/post.models';
import { UiButtonDirective } from '../../shared/ui/button/ui-button.directive';
import { UiSurfaceDirective } from '../../shared/ui/surface/ui-surface.directive';

export interface PostReportSubmission {
  readonly postId: string;
  readonly reason: string;
  readonly description: string | null;
}

@Component({
  selector: 'app-post-card',
  imports: [ReactiveFormsModule, RouterLink, UiButtonDirective, UiSurfaceDirective],
  template: `
    <article appUiSurface="default" appUiSurfacePadding="comfortable" class="grid gap-4 overflow-hidden">
      @if (primaryMediaUrl(); as mediaUrl) {
        <a class="block overflow-hidden rounded-[1.5rem] bg-[#e8e6dc]" [routerLink]="['/posts', post().id]">
          <img class="aspect-[4/3] w-full object-cover" [src]="mediaUrl" [alt]="mediaAlt()" loading="lazy" decoding="async" width="640" height="480" />
        </a>
      } @else {
        <a
          class="flex aspect-[4/3] items-center justify-center rounded-[1.5rem] border border-dashed border-[#e8e6dc] bg-[#f0eee6] text-sm font-semibold text-[#4d4c48]"
          [routerLink]="['/posts', post().id]"
        >
          Media pending
        </a>
      }

      <div class="grid gap-3">
        <div>
          <p class="text-xs font-semibold uppercase tracking-[0.22em] text-[#c96442]">Dish-first post</p>
          <a class="mt-1 block font-serif text-xl font-semibold leading-tight text-[#141413] hover:text-[#c96442]" [routerLink]="['/posts', post().id]">
            {{ dishLabel() }}
          </a>
          @if (placeLabel(); as currentPlaceLabel) {
            <p class="mt-1 text-sm font-semibold text-[#4d4c48]" [textContent]="currentPlaceLabel"></p>
          }
          <p class="mt-2 line-clamp-3 text-sm leading-6 text-[#5e5d59]" [textContent]="post().caption"></p>
        </div>

        <div class="flex flex-wrap gap-2">
          @for (tag of visibleTags(); track tag) {
            <span class="rounded-full bg-[#f0eee6] px-3 py-1 text-xs font-semibold text-[#c96442]">#{{ tag }}</span>
          }
          @if (post().priceRange) {
            <span class="rounded-full bg-[#e8e6dc] px-3 py-1 text-xs font-semibold text-[#4d4c48]" [textContent]="post().priceRange"></span>
          }
          @if (post().rating) {
            <span class="rounded-full bg-emerald-50 px-3 py-1 text-xs font-semibold text-emerald-700">{{ post().rating }}/5</span>
          }
        </div>

        <div class="flex flex-wrap items-center gap-2 text-xs font-semibold text-[#87867f]">
          <span>{{ post().likeCount }} likes</span>
          <span aria-hidden="true">·</span>
          <span>{{ post().commentCount }} comments</span>
          <span aria-hidden="true">·</span>
          <span>{{ post().repostCount }} reposts</span>
          <span aria-hidden="true">·</span>
          <span [textContent]="createdDateLabel()"></span>
        </div>

        <div class="flex flex-wrap gap-2">
          <button type="button" appUiButton="secondary" (click)="likeRequested.emit(post().id)">Like</button>
          <button type="button" appUiButton="secondary" (click)="saveRequested.emit(post().id)">Save</button>
          <button type="button" appUiButton="secondary" (click)="repostRequested.emit(post().id)">Repost</button>
          <button type="button" appUiButton="secondary" (click)="shareRequested.emit(post().id)">Share</button>
          <button type="button" appUiButton="secondary" (click)="toggleReportForm()">Report</button>
          <a appUiButton="accent" [routerLink]="['/posts', post().id]">Open</a>
        </div>

        @if (isReportFormOpen()) {
          <form class="grid gap-3 rounded-2xl border border-[#e8e6dc] bg-[#f0eee6] p-4" (ngSubmit)="submitReport()">
            <label class="grid gap-2 text-sm font-semibold text-[#141413]">
              Report reason
              <select class="min-h-11 rounded-2xl border border-[#e8e6dc] bg-[#faf9f5] px-4 text-sm font-medium text-[#141413] outline-none transition focus:border-[#d1cfc5] focus:ring-2 focus:ring-[#3898ec]/20" [formControl]="reportReasonControl">
                <option value="spam_or_low_quality">Spam or low quality</option>
                <option value="unsafe_or_harmful">Unsafe or harmful</option>
                <option value="incorrect_food_information">Incorrect food information</option>
                <option value="harassment_or_abuse">Harassment or abuse</option>
              </select>
            </label>
            <label class="grid gap-2 text-sm font-semibold text-[#141413]">
              Report note
              <textarea class="min-h-24 rounded-2xl border border-[#e8e6dc] bg-[#faf9f5] px-4 py-3 text-sm font-medium leading-6 text-[#141413] outline-none transition focus:border-[#d1cfc5] focus:ring-2 focus:ring-[#3898ec]/20" [formControl]="reportDescriptionControl" rows="3" placeholder="Add context for manual review."></textarea>
            </label>
            <div class="flex flex-wrap gap-2">
              <button type="submit" appUiButton="accent" [disabled]="reportReasonControl.invalid || reportDescriptionControl.invalid">Submit report</button>
              <button type="button" appUiButton="secondary" (click)="toggleReportForm()">Cancel</button>
            </div>
          </form>
        }
      </div>
    </article>
  `,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class PostCardComponent {
  private readonly catalogLabels = inject(CatalogLabelService);
  readonly post = input.required<PostSummaryResponse>();
  readonly likeRequested = output<string>();
  readonly saveRequested = output<string>();
  readonly repostRequested = output<string>();
  readonly shareRequested = output<string>();
  readonly reportRequested = output<PostReportSubmission>();

  protected readonly isReportFormOpen = signal(false);
  protected readonly reportReasonControl = new FormControl('spam_or_low_quality', { nonNullable: true, validators: [Validators.required] });
  protected readonly reportDescriptionControl = new FormControl('', { nonNullable: true, validators: [Validators.maxLength(500)] });
  protected readonly primaryMediaUrl = computed(() => this.post().primaryMediaUrl?.trim() || null);
  protected readonly visibleTags = computed(() => this.post().tags.slice(0, 4));
  protected readonly dishLabel = computed(() => this.catalogLabels.dishLabel(this.post().dishId));
  protected readonly placeLabel = computed(() => {
    const placeId = this.post().placeId;
    return placeId ? this.catalogLabels.placeLabel(placeId) : null;
  });
  protected readonly mediaAlt = computed(() => `Food photo for ${this.dishLabel()}`);
  protected readonly createdDateLabel = computed(() => new Date(this.post().createdAtUtc).toLocaleDateString());

  constructor() {
    effect(() => {
      const currentPost = this.post();
      this.catalogLabels.resolveDish(currentPost.dishId);
      if (currentPost.placeId) {
        this.catalogLabels.resolvePlace(currentPost.placeId);
      }
    });
  }

  protected toggleReportForm(): void {
    this.isReportFormOpen.update((isOpen) => !isOpen);
  }

  protected submitReport(): void {
    if (this.reportReasonControl.invalid || this.reportDescriptionControl.invalid) {
      this.reportReasonControl.markAsTouched();
      this.reportDescriptionControl.markAsTouched();
      return;
    }

    this.reportRequested.emit({
      postId: this.post().id,
      reason: this.reportReasonControl.value,
      description: this.trimToNull(this.reportDescriptionControl.value),
    });
    this.isReportFormOpen.set(false);
    this.reportDescriptionControl.reset('');
  }

  private trimToNull(value: string): string | null {
    const trimmedValue = value.trim();
    return trimmedValue.length > 0 ? trimmedValue : null;
  }
}

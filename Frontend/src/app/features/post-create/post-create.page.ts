import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { RouterLink } from '@angular/router';

import { DishSummaryResponse } from '../../core/dishes/dish.models';
import { PlaceSummaryResponse } from '../../core/places/place.models';
import { PostApi } from '../../core/posts/infrastructure/post.api';
import { CreatePostRequest, PostDetailResponse } from '../../core/posts/post.models';
import { DishSearchSelectComponent } from '../dishes/dish-search-select.component';
import { PlaceSearchSelectComponent } from '../places/place-search-select.component';
import { UiButtonDirective } from '../../shared/ui/button/ui-button.directive';
import { UiPageScaffoldComponent } from '../../shared/ui/page-scaffold/ui-page-scaffold.component';
import { UiSurfaceDirective } from '../../shared/ui/surface/ui-surface.directive';

type PublishStatus = 'idle' | 'pending' | 'success' | 'error';

@Component({
  selector: 'app-post-create-page',
  imports: [ReactiveFormsModule, RouterLink, DishSearchSelectComponent, PlaceSearchSelectComponent, UiButtonDirective, UiPageScaffoldComponent, UiSurfaceDirective],
  template: `
    <app-ui-page-scaffold
      eyebrow="Authenticated food post"
      title="Create post"
      description="Publish a food-first post with a canonical dish, primary media URL, and short caption."
    >
      <div class="mt-8 grid gap-4 lg:grid-cols-[minmax(0,1fr)_20rem] lg:items-start">
        <form appUiSurface="default" appUiSurfacePadding="comfortable" class="grid gap-6" [formGroup]="postForm" (ngSubmit)="publishPost()">
          <div>
            <p class="text-xs font-semibold uppercase tracking-[0.22em] text-orange-600">Publish flow</p>
            <h2 class="mt-2 text-xl font-semibold tracking-tight text-slate-950">Canonical dish first, place optional</h2>
            <p class="mt-2 text-sm leading-6 text-slate-600">
              This MVP form submits to the real BFF Post facade. Binary upload storage is still a later media slice, so the primary image is captured as a URL with lifecycle limits stated clearly.
            </p>
          </div>

          <section class="grid gap-5 xl:grid-cols-2">
            <app-dish-search-select initialQuery="phở" (dishSelected)="selectDish($event)" />
            <app-place-search-select initialQuery="quán" (placeSelected)="selectPlace($event)" />
          </section>

          <div class="grid gap-4 md:grid-cols-2">
            <article class="rounded-2xl border border-slate-200 bg-slate-50 px-4 py-3">
              <p class="text-sm font-semibold text-slate-950">Selected dish</p>
              @if (selectedDish(); as dish) {
                <p class="mt-1 text-sm text-slate-600" [textContent]="dish.name"></p>
              } @else {
                <p class="mt-1 text-sm text-red-600">Required before publish.</p>
              }
            </article>
            <article class="rounded-2xl border border-slate-200 bg-slate-50 px-4 py-3">
              <p class="text-sm font-semibold text-slate-950">Selected place</p>
              @if (selectedPlace(); as place) {
                <p class="mt-1 text-sm text-slate-600">
                  <span [textContent]="place.name"></span>
                  <span aria-hidden="true"> · </span>
                  <span [textContent]="place.area"></span>
                </p>
              } @else {
                <p class="mt-1 text-sm text-slate-500">Optional for home-cooked or unknown-place posts.</p>
              }
            </article>
          </div>

          <label class="grid gap-2 text-sm font-semibold text-slate-950">
            Primary media URL
            <input
              class="min-h-11 rounded-2xl border border-slate-200 bg-white px-4 text-sm font-medium text-slate-950 outline-none transition focus:border-orange-300 focus:ring-2 focus:ring-orange-100"
              type="url"
              formControlName="primaryMediaUrl"
              placeholder="https://example.com/pho.jpg"
            />
            @if (postForm.controls.primaryMediaUrl.invalid && postForm.controls.primaryMediaUrl.touched) {
              <span class="text-xs font-semibold text-red-600">A valid http or https primary media URL is required.</span>
            }
          </label>

          <label class="grid gap-2 text-sm font-semibold text-slate-950">
            Caption or quick review
            <textarea
              class="min-h-32 rounded-2xl border border-slate-200 bg-white px-4 py-3 text-sm font-medium leading-6 text-slate-950 outline-none transition focus:border-orange-300 focus:ring-2 focus:ring-orange-100"
              formControlName="caption"
              rows="5"
              placeholder="What should someone know before trying this?"
            ></textarea>
            @if (postForm.controls.caption.invalid && postForm.controls.caption.touched) {
              <span class="text-xs font-semibold text-red-600">Write 1 to 500 useful characters.</span>
            }
          </label>

          <div class="grid gap-4 md:grid-cols-3">
            <label class="grid gap-2 text-sm font-semibold text-slate-950">
              Rating
              <input
                class="min-h-11 rounded-2xl border border-slate-200 bg-white px-4 text-sm font-medium text-slate-950 outline-none transition focus:border-orange-300 focus:ring-2 focus:ring-orange-100"
                type="number"
                min="1"
                max="5"
                formControlName="rating"
                placeholder="1-5"
              />
            </label>
            <label class="grid gap-2 text-sm font-semibold text-slate-950">
              Price range
              <input
                class="min-h-11 rounded-2xl border border-slate-200 bg-white px-4 text-sm font-medium text-slate-950 outline-none transition focus:border-orange-300 focus:ring-2 focus:ring-orange-100"
                type="text"
                formControlName="priceRange"
                placeholder="$, $$, $$$"
              />
            </label>
            <label class="grid gap-2 text-sm font-semibold text-slate-950">
              Location text
              <input
                class="min-h-11 rounded-2xl border border-slate-200 bg-white px-4 text-sm font-medium text-slate-950 outline-none transition focus:border-orange-300 focus:ring-2 focus:ring-orange-100"
                type="text"
                formControlName="locationText"
                placeholder="District 1, HCMC"
              />
            </label>
          </div>

          <label class="grid gap-2 text-sm font-semibold text-slate-950">
            Tags
            <input
              class="min-h-11 rounded-2xl border border-slate-200 bg-white px-4 text-sm font-medium text-slate-950 outline-none transition focus:border-orange-300 focus:ring-2 focus:ring-orange-100"
              type="text"
              formControlName="tags"
              placeholder="pho, street-food, spicy"
            />
            <span class="text-xs font-medium text-slate-500">Comma-separated tags.</span>
          </label>

          @if (publishStatus() === 'success' && createdPost(); as post) {
            <p class="rounded-2xl border border-emerald-200 bg-emerald-50 px-4 py-3 text-sm font-semibold text-emerald-800">
              Post published. <a class="underline" [routerLink]="['/posts', post.id]">Open it now</a>.
            </p>
          }

          @if (publishStatus() === 'error') {
            <p class="rounded-2xl border border-red-200 bg-red-50 px-4 py-3 text-sm font-semibold text-red-700">
              Publish failed. Confirm the dish is active, the optional place is active, and the media URL is reachable.
            </p>
          }

          <div class="flex flex-wrap items-center gap-3">
            <button type="submit" appUiButton="accent" [disabled]="!canPublish()">
              @if (publishStatus() === 'pending') {
                Publishing
              } @else {
                Publish post
              }
            </button>
            <a appUiButton="secondary" routerLink="/posts">Browse posts</a>
          </div>
        </form>

        <aside appUiSurface="soft" appUiSurfacePadding="comfortable" class="h-fit">
          <p class="text-sm font-semibold text-slate-950">Still not full media lifecycle</p>
          <ul class="mt-3 space-y-3 text-sm leading-6 text-slate-600">
            <li>Original/thumbnail storage, derivatives, and cleanup jobs remain backend media work.</li>
            <li>This route publishes valid Post.Api media references through URL input.</li>
            <li>Post moderation remains manual-review-first after reports.</li>
          </ul>
        </aside>
      </div>
    </app-ui-page-scaffold>
  `,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class PostCreatePage {
  private readonly postApi = inject(PostApi);

  protected readonly selectedDish = signal<DishSummaryResponse | null>(null);
  protected readonly selectedPlace = signal<PlaceSummaryResponse | null>(null);
  protected readonly publishStatus = signal<PublishStatus>('idle');
  protected readonly createdPost = signal<PostDetailResponse | null>(null);

  protected readonly postForm = new FormGroup({
    primaryMediaUrl: new FormControl('', {
      nonNullable: true,
      validators: [Validators.required, Validators.pattern(/^https?:\/\/.+/u)],
    }),
    caption: new FormControl('', { nonNullable: true, validators: [Validators.required, Validators.maxLength(500)] }),
    rating: new FormControl<number | null>(null, { validators: [Validators.min(1), Validators.max(5)] }),
    priceRange: new FormControl('', { nonNullable: true, validators: [Validators.maxLength(20)] }),
    locationText: new FormControl('', { nonNullable: true, validators: [Validators.maxLength(120)] }),
    tags: new FormControl('', { nonNullable: true }),
  });


  protected selectDish(dish: DishSummaryResponse): void {
    this.selectedDish.set(dish);
  }

  protected selectPlace(place: PlaceSummaryResponse): void {
    this.selectedPlace.set(place);
  }

  protected canPublish(): boolean {
    return this.postForm.valid && this.selectedDish() !== null && this.publishStatus() !== 'pending';
  }

  protected publishPost(): void {
    if (!this.canPublish()) {
      this.postForm.markAllAsTouched();
      return;
    }

    this.publishStatus.set('pending');
    this.createdPost.set(null);
    this.postApi.createPost(this.buildCreatePostRequest()).subscribe({
      next: (post) => {
        this.createdPost.set(post);
        this.publishStatus.set('success');
      },
      error: () => {
        this.publishStatus.set('error');
      },
    });
  }

  private buildCreatePostRequest(): CreatePostRequest {
    const dish = this.selectedDish();
    if (dish === null) {
      throw new Error('Dish is required before publishing.');
    }

    return {
      authorId: '00000000-0000-0000-0000-000000000000',
      dishId: dish.id,
      placeId: this.selectedPlace()?.id ?? null,
      caption: this.postForm.controls.caption.value.trim(),
      rating: this.postForm.controls.rating.value,
      priceRange: this.trimToNull(this.postForm.controls.priceRange.value),
      locationText: this.trimToNull(this.postForm.controls.locationText.value),
      tags: this.parseTags(this.postForm.controls.tags.value),
      media: [
        {
          url: this.postForm.controls.primaryMediaUrl.value.trim(),
          type: 'image',
          thumbnailUrl: null,
          caption: null,
          isPrimary: true,
        },
      ],
    };
  }

  private parseTags(value: string): string[] {
    return value
      .split(',')
      .map((tag) => tag.trim().replace(/^#/u, ''))
      .filter((tag) => tag.length > 0);
  }

  private trimToNull(value: string): string | null {
    const trimmedValue = value.trim();
    return trimmedValue.length > 0 ? trimmedValue : null;
  }
}

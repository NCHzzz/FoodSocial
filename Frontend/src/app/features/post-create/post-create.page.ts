import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { RouterLink } from '@angular/router';

import { DishSummaryResponse } from '../../core/dishes/dish.models';
import { PlaceSummaryResponse } from '../../core/places/place.models';
import { PostApi } from '../../core/posts/infrastructure/post.api';
import { CreatePostRequest, MediaUploadResponse, PostDetailResponse } from '../../core/posts/post.models';
import { DishSearchSelectComponent } from '../dishes/dish-search-select.component';
import { PlaceSearchSelectComponent } from '../places/place-search-select.component';
import { UiButtonDirective } from '../../shared/ui/button/ui-button.directive';
import { UiPageScaffoldComponent } from '../../shared/ui/page-scaffold/ui-page-scaffold.component';
import { UiSurfaceDirective } from '../../shared/ui/surface/ui-surface.directive';

type PublishStatus = 'idle' | 'pending' | 'success' | 'error';
type MediaUploadStatus = 'idle' | 'pending' | 'success' | 'error';

@Component({
  selector: 'app-post-create-page',
  imports: [ReactiveFormsModule, RouterLink, DishSearchSelectComponent, PlaceSearchSelectComponent, UiButtonDirective, UiPageScaffoldComponent, UiSurfaceDirective],
  template: `
    <app-ui-page-scaffold
      eyebrow="Authenticated food post"
      title="Create post"
      description="Publish a food-first post with a canonical dish, uploaded or URL-based primary media, and short caption."
    >
      <div class="mt-8 grid gap-4 lg:grid-cols-[minmax(0,1fr)_20rem] lg:items-start">
        <form appUiSurface="default" appUiSurfacePadding="comfortable" class="grid gap-6" [formGroup]="postForm" (ngSubmit)="publishPost()">
          <div>
            <p class="text-xs font-semibold uppercase tracking-[0.22em] text-[#c96442]">Publish flow</p>
            <h2 class="mt-2 text-xl font-semibold tracking-tight text-[#141413]">Canonical dish first, place optional</h2>
            <p class="mt-2 text-sm leading-6 text-[#5e5d59]">
              This MVP form submits to the real BFF Post facade. Upload an image through the BFF media pipeline or paste a bounded media URL before publishing.
            </p>
          </div>

          <section class="grid gap-5 xl:grid-cols-2">
            <app-dish-search-select initialQuery="phở" (dishSelected)="selectDish($event)" />
            <app-place-search-select initialQuery="quán" (placeSelected)="selectPlace($event)" />
          </section>

          <div class="grid gap-4 md:grid-cols-2">
            <article class="rounded-2xl border border-[#e8e6dc] bg-[#faf9f5] px-4 py-3">
              <p class="text-sm font-semibold text-[#141413]">Selected dish</p>
              @if (selectedDish(); as dish) {
                <p class="mt-1 text-sm text-[#5e5d59]" [textContent]="dish.name"></p>
              } @else {
                <p class="mt-1 text-sm text-red-600">Required before publish.</p>
              }
            </article>
            <article class="rounded-2xl border border-[#e8e6dc] bg-[#faf9f5] px-4 py-3">
              <p class="text-sm font-semibold text-[#141413]">Selected place</p>
              @if (selectedPlace(); as place) {
                <p class="mt-1 text-sm text-[#5e5d59]">
                  <span [textContent]="place.name"></span>
                  <span aria-hidden="true"> · </span>
                  <span [textContent]="place.area"></span>
                </p>
              } @else {
                <p class="mt-1 text-sm text-[#87867f]">Optional for home-cooked or unknown-place posts.</p>
              }
            </article>
          </div>

          <section class="grid gap-3 rounded-2xl border border-[#e8e6dc] bg-[#faf9f5] px-4 py-3">
            <label class="grid gap-2 text-sm font-semibold text-[#141413]">
              Upload primary image
              <input
                class="min-h-11 rounded-2xl border border-[#e8e6dc] bg-[#faf9f5] px-4 py-2 text-sm font-medium text-[#141413] outline-none transition file:mr-4 file:rounded-full file:border-0 file:bg-[#f0eee6] file:px-4 file:py-2 file:text-sm file:font-semibold file:text-[#c96442] focus:border-[#d1cfc5] focus:ring-2 focus:ring-[#3898ec]/20"
                type="file"
                accept="image/jpeg,image/png,image/webp,.jpg,.jpeg,.png,.webp"
                (change)="uploadMedia($event)"
              />
            </label>
            @if (mediaUploadStatus() === 'pending') {
              <p class="text-xs font-semibold text-amber-700">Uploading primary image.</p>
            }
            @if (mediaUploadStatus() === 'success') {
              <p class="text-xs font-semibold text-emerald-700">Image stored with original and thumbnail references.</p>
            }
            @if (mediaUploadStatus() === 'error') {
              <p class="text-xs font-semibold text-red-600">Image upload failed. Paste a media URL or retry.</p>
            }
          </section>

          <label class="grid gap-2 text-sm font-semibold text-[#141413]">
            Primary media URL
            <input
              class="min-h-11 rounded-2xl border border-[#e8e6dc] bg-[#faf9f5] px-4 text-sm font-medium text-[#141413] outline-none transition focus:border-[#d1cfc5] focus:ring-2 focus:ring-[#3898ec]/20"
              type="text"
              formControlName="primaryMediaUrl"
              placeholder="/api/media/uploads/.../original or https://example.com/pho.jpg"
            />
            @if (postForm.controls.primaryMediaUrl.invalid && postForm.controls.primaryMediaUrl.touched) {
              <span class="text-xs font-semibold text-red-600">A BFF media path or valid http/https media URL is required.</span>
            }
          </label>

          <label class="grid gap-2 text-sm font-semibold text-[#141413]">
            Additional media URLs
            <textarea
              class="min-h-24 rounded-2xl border border-[#e8e6dc] bg-[#faf9f5] px-4 py-3 text-sm font-medium leading-6 text-[#141413] outline-none transition focus:border-[#d1cfc5] focus:ring-2 focus:ring-[#3898ec]/20"
              formControlName="additionalMediaUrls"
              rows="3"
              placeholder="One image URL per line for gallery photos."
            ></textarea>
            <span class="text-xs font-medium text-[#87867f]">Optional gallery photos stay attached to the post media list.</span>
          </label>

          <label class="grid gap-2 text-sm font-semibold text-[#141413]">
            Caption or quick review
            <textarea
              class="min-h-32 rounded-2xl border border-[#e8e6dc] bg-[#faf9f5] px-4 py-3 text-sm font-medium leading-6 text-[#141413] outline-none transition focus:border-[#d1cfc5] focus:ring-2 focus:ring-[#3898ec]/20"
              formControlName="caption"
              rows="5"
              placeholder="What should someone know before trying this?"
            ></textarea>
            @if (postForm.controls.caption.invalid && postForm.controls.caption.touched) {
              <span class="text-xs font-semibold text-red-600">Write 1 to 500 useful characters.</span>
            }
          </label>

          <div class="grid gap-4 md:grid-cols-3">
            <label class="grid gap-2 text-sm font-semibold text-[#141413]">
              Rating
              <input
                class="min-h-11 rounded-2xl border border-[#e8e6dc] bg-[#faf9f5] px-4 text-sm font-medium text-[#141413] outline-none transition focus:border-[#d1cfc5] focus:ring-2 focus:ring-[#3898ec]/20"
                type="number"
                min="1"
                max="5"
                formControlName="rating"
                placeholder="1-5"
              />
            </label>
            <label class="grid gap-2 text-sm font-semibold text-[#141413]">
              Price range
              <input
                class="min-h-11 rounded-2xl border border-[#e8e6dc] bg-[#faf9f5] px-4 text-sm font-medium text-[#141413] outline-none transition focus:border-[#d1cfc5] focus:ring-2 focus:ring-[#3898ec]/20"
                type="text"
                formControlName="priceRange"
                placeholder="$, $$, $$$"
              />
            </label>
            <label class="grid gap-2 text-sm font-semibold text-[#141413]">
              Location text
              <input
                class="min-h-11 rounded-2xl border border-[#e8e6dc] bg-[#faf9f5] px-4 text-sm font-medium text-[#141413] outline-none transition focus:border-[#d1cfc5] focus:ring-2 focus:ring-[#3898ec]/20"
                type="text"
                formControlName="locationText"
                placeholder="District 1, HCMC"
              />
            </label>
          </div>

          <label class="grid gap-2 text-sm font-semibold text-[#141413]">
            Tags
            <input
              class="min-h-11 rounded-2xl border border-[#e8e6dc] bg-[#faf9f5] px-4 text-sm font-medium text-[#141413] outline-none transition focus:border-[#d1cfc5] focus:ring-2 focus:ring-[#3898ec]/20"
              type="text"
              formControlName="tags"
              placeholder="pho, street-food, spicy"
            />
            <span class="text-xs font-medium text-[#87867f]">Comma-separated tags.</span>
          </label>

          <section class="grid gap-4 rounded-2xl border border-[#e8e6dc] bg-[#faf9f5] px-4 py-4">
            <div>
              <p class="text-sm font-semibold text-[#141413]">Recipe notes</p>
              <p class="mt-1 text-xs font-medium text-[#87867f]">Use this for home-cooked dishes. MVP stores recipe context in caption and tags until a dedicated recipe contract exists.</p>
            </div>
            <label class="grid gap-2 text-sm font-semibold text-[#141413]">
              Recipe title
              <input class="min-h-11 rounded-2xl border border-[#e8e6dc] bg-[#faf9f5] px-4 text-sm font-medium text-[#141413] outline-none transition focus:border-[#d1cfc5] focus:ring-2 focus:ring-[#3898ec]/20" type="text" formControlName="recipeTitle" placeholder="Grandma's canh chua" />
            </label>
            <label class="grid gap-2 text-sm font-semibold text-[#141413]">
              Ingredients
              <textarea class="min-h-24 rounded-2xl border border-[#e8e6dc] bg-[#faf9f5] px-4 py-3 text-sm font-medium leading-6 text-[#141413] outline-none transition focus:border-[#d1cfc5] focus:ring-2 focus:ring-[#3898ec]/20" formControlName="recipeIngredients" rows="3" placeholder="Tamarind, fish, pineapple, tomato..."></textarea>
            </label>
            <label class="grid gap-2 text-sm font-semibold text-[#141413]">
              Steps
              <textarea class="min-h-28 rounded-2xl border border-[#e8e6dc] bg-[#faf9f5] px-4 py-3 text-sm font-medium leading-6 text-[#141413] outline-none transition focus:border-[#d1cfc5] focus:ring-2 focus:ring-[#3898ec]/20" formControlName="recipeSteps" rows="4" placeholder="1. Simmer broth&#10;2. Add fish&#10;3. Finish with herbs"></textarea>
            </label>
          </section>

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
          <p class="text-sm font-semibold text-[#141413]">Still not full media lifecycle</p>
          <ul class="mt-3 space-y-3 text-sm leading-6 text-[#5e5d59]">
            <li>Uploads now store original and thumbnail references through the BFF/Post.Api media path.</li>
            <li>Cleanup expectations are recorded on upload responses; deeper async cleanup jobs remain future hardening.</li>
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
  protected readonly mediaUploadStatus = signal<MediaUploadStatus>('idle');
  protected readonly createdPost = signal<PostDetailResponse | null>(null);
  protected readonly uploadedMedia = signal<MediaUploadResponse | null>(null);

  protected readonly postForm = new FormGroup({
    primaryMediaUrl: new FormControl('', {
      nonNullable: true,
      validators: [Validators.required, Validators.pattern(/^(https?:\/\/.+|\/api\/media\/uploads\/.+)/u)],
    }),
    caption: new FormControl('', { nonNullable: true, validators: [Validators.required, Validators.maxLength(500)] }),
    additionalMediaUrls: new FormControl('', { nonNullable: true, validators: [Validators.maxLength(1000)] }),
    rating: new FormControl<number | null>(null, { validators: [Validators.min(1), Validators.max(5)] }),
    priceRange: new FormControl('', { nonNullable: true, validators: [Validators.maxLength(20)] }),
    locationText: new FormControl('', { nonNullable: true, validators: [Validators.maxLength(120)] }),
    tags: new FormControl('', { nonNullable: true }),
    recipeTitle: new FormControl('', { nonNullable: true, validators: [Validators.maxLength(120)] }),
    recipeIngredients: new FormControl('', { nonNullable: true, validators: [Validators.maxLength(500)] }),
    recipeSteps: new FormControl('', { nonNullable: true, validators: [Validators.maxLength(1000)] }),
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

  protected uploadMedia(event: Event): void {
    const input = event.target instanceof HTMLInputElement ? event.target : null;
    const file = input?.files?.item(0) ?? null;
    if (file === null) return;

    this.mediaUploadStatus.set('pending');
    this.postApi.uploadMedia(file).subscribe({
      next: (upload) => {
        this.uploadedMedia.set(upload);
        this.postForm.controls.primaryMediaUrl.setValue(upload.url);
        this.mediaUploadStatus.set('success');
      },
      error: () => {
        this.mediaUploadStatus.set('error');
      },
    });
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
      caption: this.buildCaption(),
      rating: this.postForm.controls.rating.value,
      priceRange: this.trimToNull(this.postForm.controls.priceRange.value),
      locationText: this.trimToNull(this.postForm.controls.locationText.value),
      tags: this.buildTags(),
      media: [
        {
          url: this.postForm.controls.primaryMediaUrl.value.trim(),
          type: 'image',
          thumbnailUrl: this.resolvePrimaryThumbnailUrl(),
          caption: null,
          isPrimary: true,
        },
        ...this.parseAdditionalMediaUrls().map((url) => ({
          url,
          type: 'image',
          thumbnailUrl: null,
          caption: null,
          isPrimary: false,
        })),
      ],
    };
  }

  private buildCaption(): string {
    const baseCaption = this.postForm.controls.caption.value.trim();
    const recipeTitle = this.trimToNull(this.postForm.controls.recipeTitle.value);
    const ingredients = this.trimToNull(this.postForm.controls.recipeIngredients.value);
    const steps = this.trimToNull(this.postForm.controls.recipeSteps.value);
    const recipeLines = [
      recipeTitle ? `Recipe: ${recipeTitle}` : null,
      ingredients ? `Ingredients: ${ingredients}` : null,
      steps ? `Steps: ${steps}` : null,
    ].filter((line): line is string => line !== null);

    return recipeLines.length > 0 ? `${baseCaption}\n\n${recipeLines.join('\n')}` : baseCaption;
  }

  private buildTags(): string[] {
    const tags = this.parseTags(this.postForm.controls.tags.value);
    const hasRecipeNotes = [this.postForm.controls.recipeTitle.value, this.postForm.controls.recipeIngredients.value, this.postForm.controls.recipeSteps.value]
      .some((value) => value.trim().length > 0);

    return hasRecipeNotes && !tags.includes('recipe') ? [...tags, 'recipe'] : tags;
  }

  private parseTags(value: string): string[] {
    return value
      .split(',')
      .map((tag) => tag.trim().replace(/^#/u, ''))
      .filter((tag) => tag.length > 0);
  }

  private parseAdditionalMediaUrls(): string[] {
    return this.postForm.controls.additionalMediaUrls.value
      .split(/\r?\n/u)
      .map((url) => url.trim())
      .filter((url) => /^(https?:\/\/.+|\/api\/media\/uploads\/.+)/u.test(url));
  }

  private trimToNull(value: string): string | null {
    const trimmedValue = value.trim();
    return trimmedValue.length > 0 ? trimmedValue : null;
  }

  private resolvePrimaryThumbnailUrl(): string | null {
    const uploadedMedia = this.uploadedMedia();
    const primaryUrl = this.postForm.controls.primaryMediaUrl.value.trim();
    return uploadedMedia !== null && uploadedMedia.url === primaryUrl ? uploadedMedia.thumbnailUrl : null;
  }
}

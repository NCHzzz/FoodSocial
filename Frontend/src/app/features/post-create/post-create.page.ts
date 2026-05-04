import { ChangeDetectionStrategy, Component, signal } from '@angular/core';
import { RouterLink } from '@angular/router';

import { DishSummaryResponse } from '../../core/dishes/dish.models';
import { PlaceSummaryResponse } from '../../core/places/place.models';
import { DishSearchSelectComponent } from '../dishes/dish-search-select.component';
import { PlaceSearchSelectComponent } from '../places/place-search-select.component';
import { UiButtonDirective } from '../../shared/ui/button/ui-button.directive';
import { UiPageScaffoldComponent } from '../../shared/ui/page-scaffold/ui-page-scaffold.component';
import { UiSurfaceDirective } from '../../shared/ui/surface/ui-surface.directive';

@Component({
  selector: 'app-post-create-page',
  imports: [RouterLink, DishSearchSelectComponent, PlaceSearchSelectComponent, UiButtonDirective, UiPageScaffoldComponent, UiSurfaceDirective],
  template: `
    <app-ui-page-scaffold
      eyebrow="Authenticated route area"
      title="Create post"
      description="Create a food-first post with a clear dish identity, a primary photo, and the place context that helps other people decide what to try."
    >
      <div class="mt-8 grid gap-4 lg:grid-cols-[minmax(0,1fr)_18rem]">
        <section appUiSurface="soft" appUiSurfacePadding="comfortable">
          <p class="text-xs font-semibold uppercase tracking-[0.22em] text-orange-600">Creation status</p>
          <h2 class="mt-2 text-xl font-semibold tracking-tight text-slate-950">Publishing is locked; catalog exploration is real</h2>
          <p class="mt-3 max-w-2xl text-sm leading-6 text-slate-600">
            The approved food-first flow requires a primary photo, canonical dish identity, and short caption before publish. Media upload and publish orchestration are still locked, so this page only lets you exercise the real Dish and Place catalog selectors without saving drafts.
          </p>
          <div class="mt-5 flex flex-wrap gap-3">
            <a appUiButton="accent" routerLink="/profile">Prepare your taste profile</a>
            <a appUiButton="secondary" routerLink="/discover">Return to Discover</a>
          </div>
        </section>

        <aside appUiSurface="default" appUiSurfacePadding="comfortable">
          <p class="text-sm font-semibold text-slate-950">Publish still needs later</p>
          <ul class="mt-3 space-y-3 text-sm leading-6 text-slate-600">
            <li>Media upload persistence and publish orchestration.</li>
            <li>Caption validation and publish orchestration.</li>
            <li>Feed projection after the post is accepted.</li>
          </ul>
        </aside>
      </div>

      <section class="mt-8 grid gap-5 xl:grid-cols-2">
        <app-dish-search-select initialQuery="phở" (dishSelected)="selectDish($event)" />
        <app-place-search-select initialQuery="quán" (placeSelected)="selectPlace($event)" />
      </section>

      <section class="mt-5 rounded-[2rem] border border-slate-200 bg-white p-5 shadow-[0_18px_45px_-35px_rgba(15,23,42,0.55)] sm:p-6">
        <p class="text-xs font-semibold uppercase tracking-[0.22em] text-orange-600">Readiness summary</p>
        <h2 class="mt-2 text-xl font-semibold tracking-tight text-slate-950">Catalog context preview only</h2>
        <div class="mt-5 grid gap-3 md:grid-cols-2">
          <article class="rounded-2xl border border-slate-200 bg-slate-50 px-4 py-3">
            <p class="text-sm font-semibold text-slate-950">Dish identity</p>
            @if (selectedDish(); as dish) {
              <p class="mt-1 text-sm text-slate-600" [textContent]="dish.name"></p>
            } @else {
              <p class="mt-1 text-sm text-slate-500">Required later for publishing, but not saved from this surface.</p>
            }
          </article>
          <article class="rounded-2xl border border-slate-200 bg-slate-50 px-4 py-3">
            <p class="text-sm font-semibold text-slate-950">Place context</p>
            @if (selectedPlace(); as place) {
              <p class="mt-1 text-sm text-slate-600">
                <span [textContent]="place.name"></span>
                <span aria-hidden="true"> · </span>
                <span [textContent]="place.area"></span>
              </p>
            } @else {
              <p class="mt-1 text-sm text-slate-500">Optional in MVP, but useful when a restaurant or area matters.</p>
            }
          </article>
        </div>
        <p class="mt-5 rounded-2xl border border-amber-200 bg-amber-50 px-4 py-3 text-sm font-semibold text-amber-900">
          Publish remains disabled until media upload and publish orchestration are enabled. This page only previews canonical catalog context using existing endpoints.
        </p>
      </section>
    </app-ui-page-scaffold>
  `,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class PostCreatePage {
  protected readonly selectedDish = signal<DishSummaryResponse | null>(null);
  protected readonly selectedPlace = signal<PlaceSummaryResponse | null>(null);

  protected selectDish(dish: DishSummaryResponse): void {
    this.selectedDish.set(dish);
  }

  protected selectPlace(place: PlaceSummaryResponse): void {
    this.selectedPlace.set(place);
  }
}

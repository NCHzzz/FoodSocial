const SEARCH_PAGE_TEMPLATE = `<app-ui-page-scaffold eyebrow="Food discovery search" title="Search" description="Search posts, dishes, and places with deterministic BFF-backed retrieval plus safe AI-assist fallback hints." > <section class="mt-8 grid gap-4 lg:grid-cols-[minmax(0,1fr)_18rem]"> <form appUiSurface="soft" appUiSurfacePadding="comfortable" (ngSubmit)="runSearch()"> <p class="text-xs font-semibold uppercase tracking-[0.22em] text-[#c96442]">Search query</p> <h2 class="mt-2 text-xl font-semibold tracking-tight text-[#141413]">Food-first search across live catalogs and posts</h2> <p class="mt-2 max-w-2xl text-sm leading-6 text-[#5e5d59]"> This page keeps the MVP searchable without pretending AI query assist is ready. Search returns bounded Dish, Place, and Post results from existing BFF APIs. </p> <div class="mt-5 flex flex-col gap-3 sm:flex-row"> <label class="min-w-0 flex-1 text-sm font-semibold text-[#141413]"> What are you looking for? <input class="mt-2 min-h-11 w-full rounded-2xl border border-[#e8e6dc] bg-[#faf9f5] px-4 text-sm font-medium text-[#141413] outline-none transition focus:border-[#d1cfc5] focus:ring-2 focus:ring-[#3898ec]/20" type="search" [formControl]="queryControl" placeholder="phở, District 1, spicy noodles" /> </label> <button class="self-end" type="submit" appUiButton="accent" [disabled]="!canSearch()">Search</button> </div> </form> <aside appUiSurface="default" appUiSurfacePadding="comfortable"> <p class="text-sm font-semibold text-[#141413]">AI assist status</p> @if (assist(); as currentAssist) { <p class="mt-3 text-sm leading-6 text-[#5e5d59]" [textContent]="currentAssist.explanation"></p> <p class="mt-3 rounded-full bg-amber-50 px-3 py-2 text-xs font-semibold uppercase tracking-[0.18em] text-amber-800" [textContent]="currentAssist.assistStatus"></p> <div class="mt-4 grid gap-2"> @for (suggestion of currentAssist.suggestions.slice(0, 4); track suggestion.type + suggestion.value) { <p class="rounded-2xl border border-[#e8e6dc] bg-[#faf9f5] px-3 py-2 text-xs font-semibold text-[#4d4c48]"> <span class="text-[#c96442]" [textContent]="suggestion.type"></span> <span> · </span> <span [textContent]="suggestion.value"></span> </p> } </div> } @else if (assistDegraded()) { <p class="mt-3 rounded-2xl border border-amber-200 bg-amber-50 px-3 py-2 text-sm font-semibold text-amber-900"> AI assist is unavailable. Core Dish, Place, and Post results are still shown. </p> } @else { <p class="mt-3 text-sm leading-6 text-[#5e5d59]"> Search once to get AI-assist fallback hints. Core retrieval remains deterministic when AI providers are disabled or degraded. </p> } <a class="mt-5" appUiButton="secondary" routerLink="/discover">Back to Discover</a> </aside> </section> @if (actionStatus() === 'success') { <p class="mt-5 rounded-2xl border border-emerald-200 bg-emerald-50 px-4 py-3 text-sm font-semibold text-emerald-800" [textContent]="actionMessage()"></p> } @if (actionStatus() === 'error') { <p class="mt-5 rounded-2xl border border-red-200 bg-red-50 px-4 py-3 text-sm font-semibold text-red-700" [textContent]="actionMessage()"></p> } @if (actionStatus() === 'gated') { <p class="mt-5 rounded-2xl border border-amber-200 bg-amber-50 px-4 py-3 text-sm font-semibold text-amber-900" [textContent]="actionMessage()"></p> } <section class="mt-8"> @if (status() === 'idle') { <div appUiSurface="soft" appUiSurfacePadding="comfortable"> <p class="text-sm font-semibold text-[#141413]">Start with a dish, place, or area</p> <p class="mt-2 text-sm text-[#5e5d59]">Try “phở”, “quán”, “District 1”, or a cuisine tag.</p> </div> } @if (status() === 'loading') { <div appUiSurface="soft" appUiSurfacePadding="comfortable"> <p class="text-sm font-semibold text-[#141413]">Searching FoodSocial</p> <p class="mt-2 text-sm text-[#5e5d59]">Querying Dish, Place, and Post facades in parallel.</p> </div> } @if (status() === 'error') { <div appUiSurface="soft" appUiSurfacePadding="comfortable"> <p class="text-sm font-semibold text-[#141413]">Search could not complete</p> <p class="mt-2 text-sm text-[#5e5d59]">One or more BFF search facades failed. Retry with the same query.</p> </div> } @if (status() === 'empty') { <div appUiSurface="soft" appUiSurfacePadding="comfortable"> <p class="text-sm font-semibold text-[#141413]">No results yet</p> <p class="mt-2 text-sm text-[#5e5d59]">Try a broader dish, restaurant, area, or tag query.</p> </div> } @if (status() === 'loaded') { <div class="grid gap-6"> <section appUiSurface="default" appUiSurfacePadding="comfortable"> <p class="text-xs font-semibold uppercase tracking-[0.22em] text-[#c96442]">Dishes</p> <div class="mt-4 grid gap-3 md:grid-cols-2 xl:grid-cols-3"> @for (dish of dishes(); track dish.id) { <article class="rounded-2xl border border-[#e8e6dc] bg-[#faf9f5] px-4 py-3"> <h3 class="text-base font-semibold text-[#141413]" [textContent]="dish.name"></h3> <p class="mt-1 text-sm text-[#5e5d59]"> <span [textContent]="dish.category ?? 'Category pending'"></span> <span aria-hidden="true"> · </span> <span [textContent]="dish.cuisineType ?? 'Cuisine pending'"></span> </p> <button class="mt-3" type="button" appUiButton="secondary" (click)="saveDish(dish.id)">Save dish</button> </article> } @empty { <p class="text-sm font-semibold text-[#87867f]">No dish matches.</p> } </div> </section> <section appUiSurface="default" appUiSurfacePadding="comfortable"> <p class="text-xs font-semibold uppercase tracking-[0.22em] text-[#c96442]">Places</p> <div class="mt-4 grid gap-3 md:grid-cols-2 xl:grid-cols-3"> @for (place of places(); track place.id) { <article class="rounded-2xl border border-[#e8e6dc] bg-[#faf9f5] px-4 py-3"> <h3 class="text-base font-semibold text-[#141413]" [textContent]="place.name"></h3> <p class="mt-1 text-sm text-[#5e5d59]"> <span [textContent]="place.area"></span> <span aria-hidden="true"> · </span> <span [textContent]="place.city"></span> </p> <button class="mt-3" type="button" appUiButton="secondary" (click)="savePlace(place.id)">Save place</button> </article> } @empty { <p class="text-sm font-semibold text-[#87867f]">No place matches.</p> } </div> </section> <section> <p class="mb-4 text-xs font-semibold uppercase tracking-[0.22em] text-[#c96442]">Posts</p> <div class="grid gap-5 md:grid-cols-2 xl:grid-cols-3"> @for (post of posts(); track post.id) { <app-post-card [post]="post" (likeRequested)="likePost($event)" (saveRequested)="savePost($event)" (repostRequested)="repostPost($event)" (shareRequested)="sharePost($event)" (reportRequested)="reportPost($event)" /> } @empty { <div appUiSurface="soft" appUiSurfacePadding="comfortable"> <p class="text-sm font-semibold text-[#87867f]">No post matches.</p> </div> } </div> </section> </div> } </section> </app-ui-page-scaffold>`;

import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { ReactiveFormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';

import { UiButtonDirective } from '../../shared/ui/button/ui-button.directive';
import { UiPageScaffoldComponent } from '../../shared/ui/page-scaffold/ui-page-scaffold.component';
import { UiSurfaceDirective } from '../../shared/ui/surface/ui-surface.directive';
import { PostCardComponent } from '../posts/post-card.component';
import { SearchStateService } from './search-state.service';

@Component({
  selector: 'app-search-page',
  imports: [ReactiveFormsModule, RouterLink, UiButtonDirective, UiPageScaffoldComponent, UiSurfaceDirective, PostCardComponent],
  template: SEARCH_PAGE_TEMPLATE,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class SearchPage {
  protected readonly state = inject(SearchStateService);
  protected readonly queryControl = this.state.queryControl;
  protected readonly status = this.state.status;
  protected readonly actionStatus = this.state.actionStatus;
  protected readonly actionMessage = this.state.actionMessage;
  protected readonly dishes = this.state.dishes;
  protected readonly places = this.state.places;
  protected readonly posts = this.state.posts;
  protected readonly assist = this.state.assist;
  protected readonly assistDegraded = this.state.assistDegraded;

  protected canSearch(): boolean {
    return this.state.canSearch();
  }

  protected runSearch(): void {
    this.state.runSearch();
  }

  protected likePost(postId: string): void {
    this.state.likePost(postId);
  }

  protected savePost(postId: string): void {
    this.state.savePost(postId);
  }

  protected repostPost(postId: string): void {
    this.state.repostPost(postId);
  }

  protected saveDish(dishId: string): void {
    this.state.saveDish(dishId);
  }

  protected savePlace(placeId: string): void {
    this.state.savePlace(placeId);
  }

  protected sharePost(postId: string): void {
    this.state.sharePost(postId);
  }

  protected reportPost(postId: string): void {
    this.state.reportPost(postId);
  }
}

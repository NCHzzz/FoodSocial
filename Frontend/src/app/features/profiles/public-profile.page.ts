const PUBLIC_PROFILE_PAGE_TEMPLATE = `<app-ui-page-scaffold eyebrow="Public profile" [title]="pageTitle()" description="See a person’s taste profile and follow graph. Private saved context stays visible only to the profile owner." > <div class="mt-8 grid gap-4"> @if (loadStatus() === 'loading') { <div appUiSurface="soft" appUiSurfacePadding="comfortable"> <p class="text-sm font-semibold text-[#141413]">Loading profile</p> <p class="mt-2 text-sm leading-6 text-[#5e5d59]"> Fetching this public profile through the FoodSocial BFF. </p> </div> } @if (loadStatus() === 'error') { <div appUiSurface="soft" appUiSurfacePadding="comfortable"> <p class="text-sm font-semibold text-[#141413]">Profile could not load</p> <p class="mt-2 text-sm leading-6 text-[#5e5d59]"> Try again from Profiles. The public profile facade is reachable only through the BFF. </p> <a class="mt-4" appUiButton="secondary" routerLink="/profiles"> Try again from Profiles </a> </div> } @if (profile(); as loadedProfile) { <section class="grid gap-4 lg:grid-cols-[minmax(0,1fr)_18rem] lg:items-start"> <div appUiSurface="soft" appUiSurfacePadding="comfortable"> <div class="flex flex-col gap-5 sm:flex-row sm:items-start sm:justify-between"> <div class="flex min-w-0 items-start gap-4"> <div class="flex size-16 shrink-0 items-center justify-center rounded-full bg-[#e8e6dc] text-xl font-semibold text-[#c96442]" [textContent]="avatarInitials()" ></div> <div class="min-w-0"> <p class="text-lg font-semibold tracking-tight text-[#141413]" [textContent]="displayName()"></p> <p class="mt-1 break-all text-xs font-medium uppercase tracking-[0.2em] text-[#87867f]" [textContent]="loadedProfile.identityId"></p> <p class="mt-4 max-w-2xl text-sm leading-7 text-[#5e5d59]" [textContent]="profileBio()"></p> </div> </div> @if (isOwnProfile()) { <a appUiButton="secondary" routerLink="/profile">Edit profile</a> } @else { @if (isAuthenticated()) { <button type="button" appUiButton="accent" [disabled]="followActionStatus() === 'pending'" (click)="toggleFollow()"> @if (followActionStatus() === 'pending') { Updating } @else if (isFollowing()) { Unfollow } @else { Follow } </button> } @else { <button type="button" appUiButton="secondary" (click)="startLogin()">Log in to follow</button> } } </div> @if (followActionStatus() === 'success') { <p class="mt-5 rounded-2xl border border-emerald-200 bg-emerald-50 px-4 py-3 text-sm font-semibold text-emerald-800"> Follow state updated for this profile. </p> } @if (followActionStatus() === 'error') { <p class="mt-5 rounded-2xl border border-red-200 bg-red-50 px-4 py-3 text-sm font-semibold text-red-700"> Follow action failed. Please retry when your session is active. </p> } @if (followActionStatus() === 'gated') { <p class="mt-5 rounded-2xl border border-amber-200 bg-amber-50 px-4 py-3 text-sm font-semibold text-amber-800"> Following is available after login because it changes your account graph. </p> } </div> <div appUiSurface="subtle" appUiSurfacePadding="comfortable" class="grid gap-3"> <p class="text-sm font-semibold text-[#141413]" [textContent]="followerLabel()"></p> <p class="text-sm font-semibold text-[#141413]" [textContent]="followingLabel()"></p> <p class="text-xs font-medium leading-5 text-[#87867f]"> Counts come from the profile facade; lists below load from the follow graph endpoints. </p> </div> </section> <section appUiSurface="default" appUiSurfacePadding="comfortable"> <p class="text-xs font-semibold uppercase tracking-[0.22em] text-[#c96442]">Taste profile</p> <div class="mt-4 grid gap-4 md:grid-cols-3"> <div> <h2 class="text-sm font-semibold text-[#141413]">Preferred cuisines</h2> <p class="mt-2 text-sm leading-6 text-[#5e5d59]" [textContent]="preferredCuisinesText()"></p> </div> <div> <h2 class="text-sm font-semibold text-[#141413]">Avoids</h2> <p class="mt-2 text-sm leading-6 text-[#5e5d59]" [textContent]="dislikedIngredientsText()"></p> </div> <div> <h2 class="text-sm font-semibold text-[#141413]">Dietary notes</h2> <p class="mt-2 text-sm leading-6 text-[#5e5d59]" [textContent]="dietaryRestrictionsText()"></p> </div> </div> </section> <section class="grid gap-4 lg:grid-cols-2"> <div appUiSurface="subtle" appUiSurfacePadding="comfortable"> <h2 class="text-base font-semibold text-[#141413]">Followers</h2> <p class="mt-2 text-sm leading-6 text-[#5e5d59]">People who trust this profile’s food perspective.</p> @if (followersStatus() === 'loading') { <p class="mt-4 text-sm font-semibold text-[#5e5d59]">Loading followers...</p> } @if (followersStatus() === 'error') { <p class="mt-4 text-sm font-semibold text-red-700">Followers could not load.</p> } @if (followersStatus() === 'empty') { <p class="mt-4 text-sm font-semibold text-[#87867f]">No followers yet.</p> } @if (followersStatus() === 'loaded') { <div class="mt-4 grid gap-3"> @for (follower of followers(); track follower.identityId) { <a class="rounded-2xl border border-[#e8e6dc] bg-[#faf9f5] px-4 py-3 transition hover:border-[#d1cfc5] focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-[#141413]" [routerLink]="['/profiles', follower.identityId]"> <span class="block text-sm font-semibold text-[#141413]" [textContent]="summaryName(follower)"></span> <span class="mt-1 block break-all text-xs font-medium text-[#87867f]" [textContent]="follower.identityId"></span> </a> } </div> } </div> <div appUiSurface="subtle" appUiSurfacePadding="comfortable"> <h2 class="text-base font-semibold text-[#141413]">Following</h2> <p class="mt-2 text-sm leading-6 text-[#5e5d59]">Profiles this person follows for trusted food signals.</p> @if (followingStatus() === 'loading') { <p class="mt-4 text-sm font-semibold text-[#5e5d59]">Loading following...</p> } @if (followingStatus() === 'error') { <p class="mt-4 text-sm font-semibold text-red-700">Following could not load.</p> } @if (followingStatus() === 'empty') { <p class="mt-4 text-sm font-semibold text-[#87867f]">Not following anyone yet.</p> } @if (followingStatus() === 'loaded') { <div class="mt-4 grid gap-3"> @for (followedProfile of following(); track followedProfile.identityId) { <a class="rounded-2xl border border-[#e8e6dc] bg-[#faf9f5] px-4 py-3 transition hover:border-[#d1cfc5] focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-[#141413]" [routerLink]="['/profiles', followedProfile.identityId]"> <span class="block text-sm font-semibold text-[#141413]" [textContent]="summaryName(followedProfile)"></span> <span class="mt-1 block break-all text-xs font-medium text-[#87867f]" [textContent]="followedProfile.identityId"></span> </a> } </div> } </div> </section> <section appUiSurface="soft" appUiSurfacePadding="comfortable"> <p class="text-xs font-semibold uppercase tracking-[0.22em] text-[#c96442]">Saved context</p> <h2 class="mt-2 text-base font-semibold text-[#141413]">Saved item state</h2> <p class="mt-2 max-w-2xl text-sm leading-6 text-[#5e5d59]"> Saved items are owner-scoped because they reveal private account activity. Public viewers see profile-owned taste and follow signals instead. </p> @if (!isOwnProfile()) { <p class="mt-4 rounded-2xl border border-[#e8e6dc] bg-[#faf9f5] px-4 py-3 text-sm font-semibold text-[#5e5d59]"> Saved posts, dishes, and places are private for this profile. </p> } @else { @if (savedItemsStatus() === 'loading') { <p class="mt-4 text-sm font-semibold text-[#5e5d59]">Loading saved state...</p> } @if (savedItemsStatus() === 'error') { <p class="mt-4 text-sm font-semibold text-red-700">Saved state could not load for your profile.</p> } @if (savedItemsStatus() === 'empty') { <p class="mt-4 text-sm font-semibold text-[#87867f]">No saved posts, dishes, or places yet.</p> } @if (savedItemsStatus() === 'loaded') { <div class="mt-4 grid gap-3 text-sm sm:grid-cols-3"> <p class="rounded-2xl border border-[#e8e6dc] bg-[#faf9f5] px-4 py-3 font-semibold text-[#141413]" [textContent]="savedPostsLabel()"></p> <p class="rounded-2xl border border-[#e8e6dc] bg-[#faf9f5] px-4 py-3 font-semibold text-[#141413]" [textContent]="savedDishesLabel()"></p> <p class="rounded-2xl border border-[#e8e6dc] bg-[#faf9f5] px-4 py-3 font-semibold text-[#141413]" [textContent]="savedPlacesLabel()"></p> </div> } } </section> } </div> </app-ui-page-scaffold>`;

import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { RouterLink } from '@angular/router';

import { UserProfileSummaryDto } from '../../core/user-profiles/user-profile.models';
import { UiButtonDirective } from '../../shared/ui/button/ui-button.directive';
import { UiPageScaffoldComponent } from '../../shared/ui/page-scaffold/ui-page-scaffold.component';
import { UiSurfaceDirective } from '../../shared/ui/surface/ui-surface.directive';
import { PublicProfileStateService } from './public-profile-state.service';

@Component({
  selector: 'app-public-profile-page',
  imports: [RouterLink, UiButtonDirective, UiPageScaffoldComponent, UiSurfaceDirective],
  template: PUBLIC_PROFILE_PAGE_TEMPLATE,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class PublicProfilePage {
  protected readonly state = inject(PublicProfileStateService);
  protected readonly loadStatus = this.state.loadStatus;
  protected readonly followersStatus = this.state.followersStatus;
  protected readonly followingStatus = this.state.followingStatus;
  protected readonly savedItemsStatus = this.state.savedItemsStatus;
  protected readonly followActionStatus = this.state.followActionStatus;
  protected readonly profile = this.state.profile;
  protected readonly followers = this.state.followers;
  protected readonly following = this.state.following;
  protected readonly isFollowing = this.state.isFollowing;
  protected readonly isAuthenticated = this.state.isAuthenticated;
  protected readonly isOwnProfile = this.state.isOwnProfile;
  protected readonly pageTitle = this.state.pageTitle;
  protected readonly displayName = this.state.displayName;
  protected readonly profileBio = this.state.profileBio;
  protected readonly followerLabel = this.state.followerLabel;
  protected readonly followingLabel = this.state.followingLabel;
  protected readonly preferredCuisinesText = this.state.preferredCuisinesText;
  protected readonly dislikedIngredientsText = this.state.dislikedIngredientsText;
  protected readonly dietaryRestrictionsText = this.state.dietaryRestrictionsText;
  protected readonly avatarInitials = this.state.avatarInitials;
  protected readonly savedPostsLabel = this.state.savedPostsLabel;
  protected readonly savedDishesLabel = this.state.savedDishesLabel;
  protected readonly savedPlacesLabel = this.state.savedPlacesLabel;

  protected toggleFollow(): void {
    this.state.toggleFollow();
  }

  protected startLogin(): void {
    this.state.startLogin();
  }

  protected summaryName(profile: UserProfileSummaryDto): string {
    return this.state.summaryName(profile);
  }
}

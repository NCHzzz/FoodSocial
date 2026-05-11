import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';

import { UiButtonDirective } from '../../shared/ui/button/ui-button.directive';
import { UiPageScaffoldComponent } from '../../shared/ui/page-scaffold/ui-page-scaffold.component';
import { UiSurfaceDirective } from '../../shared/ui/surface/ui-surface.directive';

@Component({
  selector: 'app-profiles-page',
  imports: [ReactiveFormsModule, RouterLink, UiButtonDirective, UiPageScaffoldComponent, UiSurfaceDirective],
  template: `
    <app-ui-page-scaffold
      eyebrow="Public route area"
      title="Profiles"
      description="Browse public profiles, authored food posts, and the identity signals that help you decide whose taste you want to follow."
    >
      <div class="mt-8 grid gap-4 lg:grid-cols-[minmax(0,1fr)_18rem]">
        <section appUiSurface="soft" appUiSurfacePadding="comfortable" class="grid gap-5">
          <p class="text-xs font-semibold uppercase tracking-[0.22em] text-[#c96442]">Profile directory status</p>
          <h2 class="mt-2 text-xl font-semibold tracking-tight text-[#141413]">Public profile pages are ready, but search is not</h2>
          <p class="mt-3 max-w-2xl text-sm leading-6 text-[#5e5d59]">
            FoodSocial can show an individual profile when you have its identity link, including taste signals and follow graph data. A public profile directory or profile search endpoint is not available yet, so this page does not list invented people.
          </p>
          <form class="grid gap-3 rounded-2xl border border-[#e8e6dc] bg-[#faf9f5] px-4 py-4" [formGroup]="profileLookupForm" (ngSubmit)="openPublicProfile()">
            <label class="grid gap-2 text-sm font-semibold text-[#141413]">
              Open profile by identity link
              <input class="min-h-11 rounded-2xl border border-[#e8e6dc] bg-[#faf9f5] px-4 text-sm font-medium text-[#141413] outline-none transition focus:border-[#d1cfc5] focus:ring-2 focus:ring-[#3898ec]/20" type="text" formControlName="identityId" placeholder="6f4ad594-e916-4f0c-9d08-b1043e15db4b" />
              @if (profileLookupForm.controls.identityId.invalid && profileLookupForm.controls.identityId.touched) {
                <span class="text-xs font-semibold text-red-600">Paste a profile identity id to open the public page.</span>
              }
            </label>
            <button type="submit" appUiButton="accent">Open public profile</button>
          </form>
          <div class="mt-5 flex flex-wrap gap-3">
            <a appUiButton="accent" routerLink="/profile">Open your profile</a>
            <a appUiButton="secondary" routerLink="/discover">Return to Discover</a>
          </div>
        </section>

        <aside appUiSurface="default" appUiSurfacePadding="comfortable">
          <p class="text-sm font-semibold text-[#141413]">Available profile capabilities</p>
          <ul class="mt-3 space-y-3 text-sm leading-6 text-[#5e5d59]">
            <li>Authenticated users can edit basic taste-profile fields.</li>
            <li>Known public profile links can show follow graph context.</li>
            <li>Saved items stay owner-scoped and private.</li>
          </ul>
        </aside>
      </div>
    </app-ui-page-scaffold>
  `,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ProfilesPage {
  private readonly router = inject(Router);

  protected readonly profileLookupForm = new FormGroup({
    identityId: new FormControl('', {
      nonNullable: true,
      validators: [Validators.required, Validators.pattern(/^[0-9a-fA-F-]{36}$/u)],
    }),
  });

  protected openPublicProfile(): void {
    if (this.profileLookupForm.invalid) {
      this.profileLookupForm.markAsTouched();
      return;
    }

    void this.router.navigate(['/profiles', this.profileLookupForm.controls.identityId.value.trim()]);
  }
}

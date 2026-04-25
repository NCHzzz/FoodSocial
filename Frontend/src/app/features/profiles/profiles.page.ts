import { ChangeDetectionStrategy, Component } from '@angular/core';
import { RouterLink } from '@angular/router';

import { UiButtonDirective } from '../../shared/ui/button/ui-button.directive';
import { UiPageScaffoldComponent } from '../../shared/ui/page-scaffold/ui-page-scaffold.component';
import { UiSurfaceDirective } from '../../shared/ui/surface/ui-surface.directive';

@Component({
  selector: 'app-profiles-page',
  imports: [RouterLink, UiButtonDirective, UiPageScaffoldComponent, UiSurfaceDirective],
  template: `
    <app-ui-page-scaffold
      eyebrow="Public route area"
      title="Profiles"
      description="Browse public profiles, authored food posts, and the identity signals that help you decide whose taste you want to follow."
    >
      <div class="mt-8 grid gap-4 lg:grid-cols-[minmax(0,1fr)_18rem]">
        <section appUiSurface="soft" appUiSurfacePadding="comfortable">
          <p class="text-xs font-semibold uppercase tracking-[0.22em] text-orange-600">Profile directory status</p>
          <h2 class="mt-2 text-xl font-semibold tracking-tight text-slate-950">Public profile pages are ready, but search is not</h2>
          <p class="mt-3 max-w-2xl text-sm leading-6 text-slate-600">
            FoodSocial can show an individual profile when you have its identity link, including taste signals and follow graph data. A public profile directory or profile search endpoint is not available yet, so this page does not list invented people.
          </p>
          <div class="mt-5 flex flex-wrap gap-3">
            <a appUiButton="accent" routerLink="/profile">Open your profile</a>
            <a appUiButton="secondary" routerLink="/discover">Return to Discover</a>
          </div>
        </section>

        <aside appUiSurface="default" appUiSurfacePadding="comfortable">
          <p class="text-sm font-semibold text-slate-950">Available profile capabilities</p>
          <ul class="mt-3 space-y-3 text-sm leading-6 text-slate-600">
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
export class ProfilesPage {}

import { ChangeDetectionStrategy, Component } from '@angular/core';
import { RouterLink } from '@angular/router';

import { UiButtonDirective } from '../../shared/ui/button/ui-button.directive';
import { UiPageScaffoldComponent } from '../../shared/ui/page-scaffold/ui-page-scaffold.component';
import { UiSurfaceDirective } from '../../shared/ui/surface/ui-surface.directive';

@Component({
  selector: 'app-following-page',
  imports: [RouterLink, UiButtonDirective, UiPageScaffoldComponent, UiSurfaceDirective],
  template: `
    <app-ui-page-scaffold
      eyebrow="Authenticated route area"
      title="Following"
      description="Keep up with people you trust, with a calmer feed that prioritizes followed accounts over broader discovery."
    >
      <div class="mt-8 grid gap-4 lg:grid-cols-[minmax(0,1fr)_18rem]">
        <section appUiSurface="soft" appUiSurfacePadding="comfortable">
          <p class="text-xs font-semibold uppercase tracking-[0.22em] text-orange-600">Trusted feed status</p>
          <h2 class="mt-2 text-xl font-semibold tracking-tight text-slate-950">Your follow graph exists before the feed does</h2>
          <p class="mt-3 max-w-2xl text-sm leading-6 text-slate-600">
            FoodSocial can load and update user profile follow relationships through the BFF. The actual Following feed still depends on future Post and feed services, so this route stays honest instead of showing placeholder meals.
          </p>
          <div class="mt-5 flex flex-wrap gap-3">
            <a appUiButton="accent" routerLink="/profiles">Find profiles to follow</a>
            <a appUiButton="secondary" routerLink="/profile">Manage your taste profile</a>
          </div>
        </section>

        <aside appUiSurface="default" appUiSurfacePadding="comfortable">
          <p class="text-sm font-semibold text-slate-950">What will appear here later</p>
          <ul class="mt-3 space-y-3 text-sm leading-6 text-slate-600">
            <li>Posts from people you follow.</li>
            <li>Dish identity before place and engagement metadata.</li>
            <li>Calmer social provenance than Discover.</li>
          </ul>
        </aside>
      </div>
    </app-ui-page-scaffold>
  `,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class FollowingPage {}

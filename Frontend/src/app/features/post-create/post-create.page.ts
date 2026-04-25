import { ChangeDetectionStrategy, Component } from '@angular/core';
import { RouterLink } from '@angular/router';

import { UiButtonDirective } from '../../shared/ui/button/ui-button.directive';
import { UiPageScaffoldComponent } from '../../shared/ui/page-scaffold/ui-page-scaffold.component';
import { UiSurfaceDirective } from '../../shared/ui/surface/ui-surface.directive';

@Component({
  selector: 'app-post-create-page',
  imports: [RouterLink, UiButtonDirective, UiPageScaffoldComponent, UiSurfaceDirective],
  template: `
    <app-ui-page-scaffold
      eyebrow="Authenticated route area"
      title="Create post"
      description="Create a food-first post with a clear dish identity, a primary photo, and the place context that helps other people decide what to try."
    >
      <div class="mt-8 grid gap-4 lg:grid-cols-[minmax(0,1fr)_18rem]">
        <section appUiSurface="soft" appUiSurfacePadding="comfortable">
          <p class="text-xs font-semibold uppercase tracking-[0.22em] text-orange-600">Creation status</p>
          <h2 class="mt-2 text-xl font-semibold tracking-tight text-slate-950">Posting is intentionally locked until Dish and Post APIs exist</h2>
          <p class="mt-3 max-w-2xl text-sm leading-6 text-slate-600">
            The approved food-first flow requires a primary photo, canonical dish identity, and short caption before publish. Because the current backend does not yet expose Dish resolution, media publishing, or Post creation, this page explains the path without accepting fake drafts.
          </p>
          <div class="mt-5 flex flex-wrap gap-3">
            <a appUiButton="accent" routerLink="/profile">Prepare your taste profile</a>
            <a appUiButton="secondary" routerLink="/discover">Return to Discover</a>
          </div>
        </section>

        <aside appUiSurface="default" appUiSurfacePadding="comfortable">
          <p class="text-sm font-semibold text-slate-950">Publish needs later</p>
          <ul class="mt-3 space-y-3 text-sm leading-6 text-slate-600">
            <li>Canonical dish selection.</li>
            <li>Post and media persistence.</li>
            <li>Optional place context after dish identity is clear.</li>
          </ul>
        </aside>
      </div>
    </app-ui-page-scaffold>
  `,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class PostCreatePage {}

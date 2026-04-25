import { ChangeDetectionStrategy, Component } from '@angular/core';
import { RouterLink } from '@angular/router';

import { UiButtonDirective } from '../../shared/ui/button/ui-button.directive';
import { UiPageScaffoldComponent } from '../../shared/ui/page-scaffold/ui-page-scaffold.component';
import { UiSurfaceDirective } from '../../shared/ui/surface/ui-surface.directive';

@Component({
  selector: 'app-posts-page',
  imports: [RouterLink, UiButtonDirective, UiPageScaffoldComponent, UiSurfaceDirective],
  template: `
    <app-ui-page-scaffold
      eyebrow="Public route area"
      title="Posts"
      description="Explore public food posts, then move from dish curiosity into richer details, saves, and conversation when the content earns it."
    >
      <div class="mt-8 grid gap-4 lg:grid-cols-[minmax(0,1fr)_18rem]">
        <section appUiSurface="soft" appUiSurfacePadding="comfortable">
          <p class="text-xs font-semibold uppercase tracking-[0.22em] text-orange-600">Content status</p>
          <h2 class="mt-2 text-xl font-semibold tracking-tight text-slate-950">Public posts are not wired yet</h2>
          <p class="mt-3 max-w-2xl text-sm leading-6 text-slate-600">
            The current backend foundation exposes authentication, session state, and user profile capabilities. Dish, Post, Place, and feed retrieval are still unavailable, so this page does not invent sample posts or call endpoints that do not exist.
          </p>
          <div class="mt-5 flex flex-wrap gap-3">
            <a appUiButton="accent" routerLink="/discover">Review Discover framing</a>
            <a appUiButton="secondary" routerLink="/profiles">Browse profile foundation</a>
          </div>
        </section>

        <aside appUiSurface="default" appUiSurfacePadding="comfortable">
          <p class="text-sm font-semibold text-slate-950">Available today</p>
          <ul class="mt-3 space-y-3 text-sm leading-6 text-slate-600">
            <li>Public route orientation for future post browsing.</li>
            <li>Authenticated profile and saved-item identifiers where the BFF supports them.</li>
            <li>Clear unavailable state instead of fake feed data.</li>
          </ul>
        </aside>
      </div>
    </app-ui-page-scaffold>
  `,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class PostsPage {}

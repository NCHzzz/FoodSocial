import { ChangeDetectionStrategy, Component } from '@angular/core';
import { RouterLink } from '@angular/router';

import { UiButtonDirective } from '../../shared/ui/button/ui-button.directive';
import { UiPageScaffoldComponent } from '../../shared/ui/page-scaffold/ui-page-scaffold.component';
import { UiSurfaceDirective } from '../../shared/ui/surface/ui-surface.directive';

@Component({
  selector: 'app-notifications-page',
  imports: [RouterLink, UiButtonDirective, UiPageScaffoldComponent, UiSurfaceDirective],
  template: `
    <app-ui-page-scaffold
      eyebrow="Authenticated route area"
      title="Notifications"
      description="See follows, likes, comments, and repost activity in one place without losing the path back to the food and people involved."
    >
      <div class="mt-8 grid gap-4 lg:grid-cols-[minmax(0,1fr)_18rem]">
        <section appUiSurface="soft" appUiSurfacePadding="comfortable">
          <p class="text-xs font-semibold uppercase tracking-[0.22em] text-orange-600">Notification status</p>
          <h2 class="mt-2 text-xl font-semibold tracking-tight text-slate-950">No notification stream is available yet</h2>
          <p class="mt-3 max-w-2xl text-sm leading-6 text-slate-600">
            Follow, like, comment, and repost notifications are part of the MVP direction, but the current browser-facing backend does not expose a notification API. This empty state keeps the center ready without manufacturing activity.
          </p>
          <div class="mt-5 flex flex-wrap gap-3">
            <a appUiButton="accent" routerLink="/profiles">Check profile activity</a>
            <a appUiButton="secondary" routerLink="/following">View Following status</a>
          </div>
        </section>

        <aside appUiSurface="default" appUiSurfacePadding="comfortable">
          <p class="text-sm font-semibold text-slate-950">Future notification types</p>
          <ul class="mt-3 space-y-3 text-sm leading-6 text-slate-600">
            <li>Follows and profile relationships.</li>
            <li>Likes, comments, saves, and reposts.</li>
            <li>Account or moderation notices when supported.</li>
          </ul>
        </aside>
      </div>
    </app-ui-page-scaffold>
  `,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class NotificationsPage {}

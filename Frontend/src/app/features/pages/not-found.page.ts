import { ChangeDetectionStrategy, Component } from '@angular/core';
import { RouterLink } from '@angular/router';

import { UiPageScaffoldComponent } from '../../shared/ui/page-scaffold/ui-page-scaffold.component';
import { UiButtonDirective } from '../../shared/ui/button/ui-button.directive';
import { UiSurfaceDirective } from '../../shared/ui/surface/ui-surface.directive';

@Component({
  selector: 'app-not-found-page',
  imports: [RouterLink, UiPageScaffoldComponent, UiButtonDirective, UiSurfaceDirective],
  template: `
    <app-ui-page-scaffold
      eyebrow="Not found"
      title="This page does not exist"
      description="The link you followed or the address you typed does not match anything on FoodSocial."
    >
      <div class="mt-8 grid gap-4 sm:flex sm:items-center sm:gap-6">
        <a appUiButton="accent" routerLink="/discover">Back to Discover</a>
        <a appUiButton="secondary" routerLink="/search">Search dishes or places</a>
      </div>

      <div class="mt-12" appUiSurface="soft" appUiSurfacePadding="comfortable">
        <p class="text-sm font-semibold text-[#141413]">What could cause this?</p>
        <ul class="mt-3 grid gap-2 text-sm leading-6 text-[#5e5d59]">
          <li>The page may have been moved or renamed after the last update.</li>
          <li>The bookmark or link you used might point to an old location.</li>
          <li>You may not have access to the resource you requested.</li>
        </ul>
      </div>
    </app-ui-page-scaffold>
  `,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class NotFoundPage {}

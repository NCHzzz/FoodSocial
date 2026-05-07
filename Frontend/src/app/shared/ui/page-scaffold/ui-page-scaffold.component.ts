import { ChangeDetectionStrategy, Component, input } from '@angular/core';

import { UiSurfaceDirective } from '../surface/ui-surface.directive';

@Component({
  selector: 'app-ui-page-scaffold',
  imports: [UiSurfaceDirective],
  template: `
    <section appUiSurface="default" appUiSurfacePadding="spacious" appUiSurfaceRadius="feature">
      <p class="text-xs font-semibold uppercase tracking-[0.22em] text-[#c96442]" [textContent]="eyebrow()"></p>
      <h1 class="mt-3 text-3xl font-semibold tracking-tight text-[#141413]" [textContent]="title()"></h1>
      <p class="mt-4 max-w-2xl text-sm leading-7 text-[#5e5d59]" [textContent]="description()"></p>

      <ng-content />
    </section>
  `,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class UiPageScaffoldComponent {
  readonly eyebrow = input.required<string>();
  readonly title = input.required<string>();
  readonly description = input.required<string>();
}

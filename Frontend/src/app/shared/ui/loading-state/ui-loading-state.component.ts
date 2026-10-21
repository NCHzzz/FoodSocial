import { ChangeDetectionStrategy, Component, input } from '@angular/core';

import { UiSurfaceDirective } from '../surface/ui-surface.directive';

@Component({
  selector: 'app-ui-loading-state',
  imports: [UiSurfaceDirective],
  template: `
    <section appUiSurface="soft" appUiSurfacePadding="comfortable">
      <p class="text-sm font-semibold text-[#141413]" [textContent]="title()"></p>
      @if (description(); as desc) {
        <p class="mt-2 text-sm leading-6 text-[#5e5d59]" [textContent]="desc"></p>
      }
    </section>
  `,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class UiLoadingStateComponent {
  readonly title = input.required<string>();
  readonly description = input<string>();
}

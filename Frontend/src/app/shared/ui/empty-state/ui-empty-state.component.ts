import { ChangeDetectionStrategy, Component, input } from '@angular/core';

import { UiSurfaceDirective } from '../surface/ui-surface.directive';
import { UiButtonDirective } from '../button/ui-button.directive';

@Component({
  selector: 'app-ui-empty-state',
  imports: [UiSurfaceDirective, UiButtonDirective],
  template: `
    <section appUiSurface="soft" appUiSurfacePadding="comfortable">
      <p class="text-sm font-semibold text-[#141413]" [textContent]="title()"></p>
      @if (description(); as desc) {
        <p class="mt-2 text-sm leading-6 text-[#5e5d59]" [textContent]="desc"></p>
      }
      @if (retryLabel(); as label) {
        <button type="button" appUiButton="secondary" class="mt-4" (click)="retryClick()" [textContent]="label"></button>
      }
    </section>
  `,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class UiEmptyStateComponent {
  readonly title = input.required<string>();
  readonly description = input<string>();
  readonly retryLabel = input<string>();
  readonly retry = input<() => void>();

  protected retryClick(): void {
    this.retry()?.();
  }
}

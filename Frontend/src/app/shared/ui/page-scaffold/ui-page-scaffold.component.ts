import { ChangeDetectionStrategy, Component, input } from '@angular/core';

@Component({
  selector: 'app-ui-page-scaffold',
  template: `
    <section class="rounded-[2rem] border border-slate-200 bg-white p-8 shadow-sm">
      <p class="text-xs font-semibold uppercase tracking-[0.22em] text-orange-600" [textContent]="eyebrow()"></p>
      <h1 class="mt-3 text-3xl font-semibold tracking-tight text-slate-950" [textContent]="title()"></h1>
      <p class="mt-4 max-w-2xl text-sm leading-7 text-slate-600" [textContent]="description()"></p>

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

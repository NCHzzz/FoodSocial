import { Directive, HostBinding, input } from '@angular/core';

type UiButtonTone = 'primary' | 'accent' | 'secondary';

const UI_BUTTON_BASE_CLASSES =
  'inline-flex min-h-11 items-center justify-center rounded-full px-5 py-3 text-sm font-semibold transition focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 disabled:cursor-not-allowed disabled:opacity-60';

const UI_BUTTON_TONE_CLASSES: Record<UiButtonTone, string> = {
  primary: 'bg-slate-950 text-white shadow-lg shadow-slate-300 hover:bg-slate-800 focus-visible:outline-slate-950',
  accent: 'bg-orange-500 text-white shadow-lg shadow-orange-200 hover:bg-orange-400 focus-visible:outline-orange-500',
  secondary:
    'border border-slate-200 bg-white text-slate-700 shadow-sm hover:border-orange-300 hover:text-slate-950 focus-visible:outline-slate-950',
};

@Directive({
  selector: 'button[appUiButton]',
})
export class UiButtonDirective {
  readonly appUiButton = input<UiButtonTone>('primary');

  @HostBinding('class')
  get hostClasses(): string {
    return `${UI_BUTTON_BASE_CLASSES} ${UI_BUTTON_TONE_CLASSES[this.appUiButton()]}`;
  }
}

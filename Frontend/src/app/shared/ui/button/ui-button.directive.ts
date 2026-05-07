import { Directive, HostBinding, input } from '@angular/core';

type UiButtonTone = 'primary' | 'accent' | 'secondary';

const UI_BUTTON_BASE_CLASSES =
  'inline-flex min-h-11 items-center justify-center rounded-xl px-5 py-3 text-sm font-semibold transition focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 disabled:cursor-not-allowed disabled:opacity-60';

const UI_BUTTON_TONE_CLASSES: Record<UiButtonTone, string> = {
  primary: 'bg-[#141413] text-[#faf9f5] shadow-[0_0_0_1px_#c2c0b6] hover:bg-[#30302e] focus-visible:outline-[#141413]',
  accent: 'bg-[#c96442] text-[#faf9f5] shadow-[0_0_0_1px_#c96442] hover:bg-[#d97757] focus-visible:outline-[#3898ec]',
  secondary:
    'border border-[#e8e6dc] bg-[#faf9f5] text-[#4d4c48] shadow-[0_0_0_1px_#f0eee6] hover:border-[#d1cfc5] hover:text-[#141413] focus-visible:outline-[#141413]',
};

@Directive({
  selector: 'button[appUiButton], a[appUiButton]',
})
export class UiButtonDirective {
  readonly appUiButton = input<UiButtonTone>('primary');

  @HostBinding('class')
  get hostClasses(): string {
    return `${UI_BUTTON_BASE_CLASSES} ${UI_BUTTON_TONE_CLASSES[this.appUiButton()]}`;
  }
}

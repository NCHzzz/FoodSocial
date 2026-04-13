import { Directive, HostBinding, input } from '@angular/core';

type UiSurfaceTone = 'default' | 'subtle' | 'soft';
type UiSurfacePadding = 'compact' | 'comfortable';

const UI_SURFACE_BASE_CLASSES = 'block rounded-2xl border shadow-sm';

const UI_SURFACE_TONE_CLASSES: Record<UiSurfaceTone, string> = {
  default: 'border-slate-200 bg-white',
  subtle: 'border-slate-200 bg-slate-50',
  soft: 'border-slate-200 bg-slate-50/80',
};

const UI_SURFACE_PADDING_CLASSES: Record<UiSurfacePadding, string> = {
  compact: 'px-4 py-3',
  comfortable: 'p-4',
};

@Directive({
  selector: '[appUiSurface]',
})
export class UiSurfaceDirective {
  readonly appUiSurface = input<UiSurfaceTone>('default');
  readonly appUiSurfacePadding = input<UiSurfacePadding>('comfortable');

  @HostBinding('class')
  get hostClasses(): string {
    return [
      UI_SURFACE_BASE_CLASSES,
      UI_SURFACE_TONE_CLASSES[this.appUiSurface()],
      UI_SURFACE_PADDING_CLASSES[this.appUiSurfacePadding()],
    ].join(' ');
  }
}

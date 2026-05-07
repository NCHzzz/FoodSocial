import { Directive, HostBinding, input } from '@angular/core';

type UiSurfaceTone = 'default' | 'subtle' | 'soft';
type UiSurfacePadding = 'compact' | 'comfortable' | 'spacious';
type UiSurfaceRadius = 'default' | 'feature';

const UI_SURFACE_BASE_CLASSES = 'block border shadow-[0_0_0_1px_#f0eee6]';

const UI_SURFACE_TONE_CLASSES: Record<UiSurfaceTone, string> = {
  default: 'border-[#e8e6dc] bg-[#faf9f5]',
  subtle: 'border-[#e8e6dc] bg-[#faf9f5]',
  soft: 'border-[#e8e6dc] bg-[#faf9f5]/80',
};

const UI_SURFACE_PADDING_CLASSES: Record<UiSurfacePadding, string> = {
  compact: 'px-4 py-3',
  comfortable: 'p-4',
  spacious: 'p-8',
};

const UI_SURFACE_RADIUS_CLASSES: Record<UiSurfaceRadius, string> = {
  default: 'rounded-2xl',
  feature: 'rounded-[2rem]',
};

@Directive({
  selector: '[appUiSurface]',
})
export class UiSurfaceDirective {
  readonly appUiSurface = input<UiSurfaceTone>('default');
  readonly appUiSurfacePadding = input<UiSurfacePadding>('comfortable');
  readonly appUiSurfaceRadius = input<UiSurfaceRadius>('default');

  @HostBinding('class')
  get hostClasses(): string {
    return [
      UI_SURFACE_BASE_CLASSES,
      UI_SURFACE_TONE_CLASSES[this.appUiSurface()],
      UI_SURFACE_PADDING_CLASSES[this.appUiSurfacePadding()],
      UI_SURFACE_RADIUS_CLASSES[this.appUiSurfaceRadius()],
    ].join(' ');
  }
}

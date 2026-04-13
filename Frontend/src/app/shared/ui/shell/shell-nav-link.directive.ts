import { Directive, HostBinding } from '@angular/core';

const SHELL_NAV_LINK_CLASSES =
  'inline-flex min-h-11 items-center rounded-full border border-slate-200 bg-white px-4 text-sm font-medium text-slate-600 shadow-sm transition hover:border-orange-300 hover:text-slate-950';

@Directive({
  selector: 'a[appShellNavLink]',
})
export class ShellNavLinkDirective {
  @HostBinding('class')
  readonly hostClasses = SHELL_NAV_LINK_CLASSES;
}

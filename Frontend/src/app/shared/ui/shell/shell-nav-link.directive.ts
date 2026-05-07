import { Directive, HostBinding } from '@angular/core';

const SHELL_NAV_LINK_CLASSES =
  'inline-flex min-h-11 items-center rounded-full border border-[#e8e6dc] bg-[#faf9f5] px-4 text-sm font-medium text-[#5e5d59] shadow-[0_0_0_1px_#f0eee6] transition hover:border-[#d1cfc5] hover:text-[#141413] focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-[#3898ec]';

@Directive({
  selector: 'a[appShellNavLink]',
})
export class ShellNavLinkDirective {
  @HostBinding('class')
  readonly hostClasses = SHELL_NAV_LINK_CLASSES;
}

import { ChangeDetectionStrategy, Component, OnInit, computed, inject } from '@angular/core';
import { RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';

import { AuthSessionService } from '../core/auth/auth-session.service';
import { getAuthSessionBadgeClasses, getAuthSessionSummary, isAuthenticatedSession } from '../core/auth/auth-session.presentation';
import { UiButtonDirective } from '../shared/ui/button/ui-button.directive';
import { ShellNavLinkDirective } from '../shared/ui/shell/shell-nav-link.directive';
import { UiSurfaceDirective } from '../shared/ui/surface/ui-surface.directive';
import { AUTHENTICATED_NAVIGATION_ITEMS, GUEST_NAVIGATION_ITEMS } from './app-shell.navigation';

@Component({
  selector: 'app-app-shell',
  imports: [RouterLink, RouterLinkActive, RouterOutlet, ShellNavLinkDirective, UiButtonDirective, UiSurfaceDirective],
  templateUrl: './app-shell.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AppShellComponent implements OnInit {
  private readonly authSessionService = inject(AuthSessionService);

  protected readonly authState = this.authSessionService.authState;
  protected readonly isAuthenticated = computed(() => isAuthenticatedSession(this.authState()));
  protected readonly session = computed(() => this.authState().session);
  protected readonly sessionLoadFailed = computed(() => this.authState().loadStatus === 'unavailable');
  protected readonly sessionStatusClasses = computed(() => getAuthSessionBadgeClasses(this.authState()));
  protected readonly sessionSummary = computed(() => getAuthSessionSummary(this.authState()));
  protected readonly navigationItems = computed(() =>
    this.isAuthenticated() ? AUTHENTICATED_NAVIGATION_ITEMS : GUEST_NAVIGATION_ITEMS,
  );

  ngOnInit(): void {
    void this.authSessionService.loadSession();
  }

  protected startLogin(): void {
    this.authSessionService.startLogin();
  }

  protected submitLogout(): void {
    void this.authSessionService.submitLogout();
  }
}

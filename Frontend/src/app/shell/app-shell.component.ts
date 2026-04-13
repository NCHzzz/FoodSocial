import { ChangeDetectionStrategy, Component, OnInit, computed, inject } from '@angular/core';
import { RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';

import { AuthSessionService } from '../core/auth/auth-session.service';
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

  protected readonly authState = this.authSessionService.state;
  protected readonly isAuthenticated = computed(
    () => this.authState().status === 'authenticated' && this.authState().session !== null,
  );
  protected readonly session = computed(() => this.authState().session);
  protected readonly logoutAction = computed(() => this.authSessionService.buildLogoutUrl());
  protected readonly sessionSummary = computed(() => {
    const currentState = this.authState();

    if (currentState.status === 'authenticated' && currentState.session !== null) {
      return currentState.session.identity.bootstrapPending
        ? 'Your FoodSocial account is still finishing setup.'
        : 'Your FoodSocial session is active.';
    }

    if (currentState.status === 'loading') {
      return 'Checking whether you already have an active session.';
    }

    return 'Browse public food content now, then sign in when you want deeper social actions.';
  });
  protected readonly navigationItems = computed(() =>
    this.isAuthenticated() ? AUTHENTICATED_NAVIGATION_ITEMS : GUEST_NAVIGATION_ITEMS,
  );

  ngOnInit(): void {
    void this.authSessionService.loadSession();
  }

  protected startLogin(): void {
    this.authSessionService.startLogin();
  }

  protected markLogoutPending(): void {
    this.authSessionService.markLogoutPending();
  }
}

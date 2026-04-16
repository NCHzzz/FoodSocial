import { ChangeDetectionStrategy, Component, computed, inject } from '@angular/core';

import { AuthSessionService } from '../../core/auth/auth-session.service';
import {
  getAuthSessionBadgeClasses,
  getAuthSessionBadgeLabel,
  getDiscoverSessionAvailabilityMessage,
  isAuthenticatedSession,
} from '../../core/auth/auth-session.presentation';
import { UiSurfaceDirective } from '../../shared/ui/surface/ui-surface.directive';

@Component({
  selector: 'app-discover-page',
  imports: [UiSurfaceDirective],
  templateUrl: './discover.page.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class DiscoverPage {
  private readonly authSessionService = inject(AuthSessionService);

  protected readonly authState = this.authSessionService.authState;
  protected readonly isAuthenticated = computed(() => isAuthenticatedSession(this.authState()));
  protected readonly session = computed(() => this.authState().session);
  protected readonly roles = computed(() => this.session()?.user.roles ?? []);
  protected readonly sessionStatusClasses = computed(() => getAuthSessionBadgeClasses(this.authState()));
  protected readonly sessionStatusLabel = computed(() => getAuthSessionBadgeLabel(this.authState()));
  protected readonly sessionAvailabilityMessage = computed(() => getDiscoverSessionAvailabilityMessage(this.authState()));
}

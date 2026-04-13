import { ChangeDetectionStrategy, Component, computed, inject } from '@angular/core';

import { AuthSessionService } from '../../core/auth/auth-session.service';
import { UiSurfaceDirective } from '../../shared/ui/surface/ui-surface.directive';

@Component({
  selector: 'app-discover-page',
  imports: [UiSurfaceDirective],
  templateUrl: './discover.page.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class DiscoverPage {
  private readonly authSessionService = inject(AuthSessionService);

  protected readonly authState = this.authSessionService.state;
  protected readonly isAuthenticated = computed(
    () => this.authState().status === 'authenticated' && this.authState().session !== null,
  );
  protected readonly session = computed(() => this.authState().session);
  protected readonly roles = computed(() => this.session()?.user.roles ?? []);
}

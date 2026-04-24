import { ChangeDetectionStrategy, Component, computed, inject } from '@angular/core';
import { RouterLink } from '@angular/router';

import { AuthSessionService } from '../../core/auth/auth-session.service';
import { UiButtonDirective } from '../../shared/ui/button/ui-button.directive';
import { UiPageScaffoldComponent } from '../../shared/ui/page-scaffold/ui-page-scaffold.component';
import { UiSurfaceDirective } from '../../shared/ui/surface/ui-surface.directive';

@Component({
  selector: 'app-profile-page',
  imports: [RouterLink, UiButtonDirective, UiPageScaffoldComponent, UiSurfaceDirective],
  template: `
    <app-ui-page-scaffold
      eyebrow="Authenticated route area"
      title="Profile"
      description="Manage your profile, taste signals, and saved food activity from a signed-in space that stays clearly separate from public browsing."
    >
      <div class="mt-8 grid gap-4 md:grid-cols-2">
        <div appUiSurface="soft" appUiSurfacePadding="comfortable">
          <p class="text-sm font-semibold text-slate-950">Public profile preview</p>
          <p class="mt-2 text-sm leading-6 text-slate-600">
            Open the public profile route for your current session identity. Editing fields can build on
            the typed UserProfile client once form validation rules are finalized.
          </p>
          @if (ownProfileLink(); as profileLink) {
            <a class="mt-4" appUiButton="secondary" [routerLink]="profileLink">
              View public profile
            </a>
          }
        </div>

        <div appUiSurface="subtle" appUiSurfacePadding="comfortable">
          <p class="text-sm font-semibold text-slate-950">Edit foundation</p>
          <p class="mt-2 text-sm leading-6 text-slate-600">
            Next step: wire a guarded profile form to update display name, avatar URL, bio, and manual
            taste signals through PATCH /api/users/{{ ownIdentityId() ?? 'your-identity-id' }}.
          </p>
        </div>
      </div>
    </app-ui-page-scaffold>
  `,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ProfilePage {
  private readonly authSessionService = inject(AuthSessionService);

  readonly ownIdentityId = computed(() => this.authSessionService.authState().session?.identity.identityId ?? null);
  readonly ownProfileLink = computed(() => {
    const identityId = this.ownIdentityId();

    return identityId ? ['/profiles', identityId] : null;
  });
}

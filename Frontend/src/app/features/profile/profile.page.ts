import { ChangeDetectionStrategy, Component } from '@angular/core';

import { UiPageScaffoldComponent } from '../../shared/ui/page-scaffold/ui-page-scaffold.component';

@Component({
  selector: 'app-profile-page',
  imports: [UiPageScaffoldComponent],
  template: `
    <app-ui-page-scaffold
      eyebrow="Authenticated route area"
      title="Profile"
      description="Manage your profile, taste signals, and saved food activity from a signed-in space that stays clearly separate from public browsing."
    />
  `,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ProfilePage {}

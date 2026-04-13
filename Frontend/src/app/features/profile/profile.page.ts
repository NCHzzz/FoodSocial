import { ChangeDetectionStrategy, Component } from '@angular/core';

import { UiPageScaffoldComponent } from '../../shared/ui/page-scaffold/ui-page-scaffold.component';

@Component({
  selector: 'app-profile-page',
  imports: [UiPageScaffoldComponent],
  template: `
    <app-ui-page-scaffold
      eyebrow="Authenticated route area"
      title="Profile"
      description="Profile management remains a signed-in capability. This placeholder keeps the route boundary explicit and ready for the real profile MVP slice."
    />
  `,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ProfilePage {}

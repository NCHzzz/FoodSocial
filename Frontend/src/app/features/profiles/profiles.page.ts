import { ChangeDetectionStrategy, Component } from '@angular/core';

import { UiPageScaffoldComponent } from '../../shared/ui/page-scaffold/ui-page-scaffold.component';

@Component({
  selector: 'app-profiles-page',
  imports: [UiPageScaffoldComponent],
  template: `
    <app-ui-page-scaffold
      eyebrow="Public route area"
      title="Profiles"
      description="Limited public profile browsing remains visible in guest mode, while editing and deeper account actions stay behind authenticated routes."
    />
  `,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ProfilesPage {}

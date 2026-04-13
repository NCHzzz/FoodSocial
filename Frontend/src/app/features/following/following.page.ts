import { ChangeDetectionStrategy, Component } from '@angular/core';

import { UiPageScaffoldComponent } from '../../shared/ui/page-scaffold/ui-page-scaffold.component';

@Component({
  selector: 'app-following-page',
  imports: [UiPageScaffoldComponent],
  template: `
    <app-ui-page-scaffold
      eyebrow="Authenticated route area"
      title="Following"
      description="The following feed is now a dedicated lazy route area. Guests are redirected away until a verified session exists."
    />
  `,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class FollowingPage {}

import { ChangeDetectionStrategy, Component } from '@angular/core';

import { UiPageScaffoldComponent } from '../../shared/ui/page-scaffold/ui-page-scaffold.component';

@Component({
  selector: 'app-following-page',
  imports: [UiPageScaffoldComponent],
  template: `
    <app-ui-page-scaffold
      eyebrow="Authenticated route area"
      title="Following"
      description="Keep up with people you trust, with a calmer feed that prioritizes followed accounts over broader discovery."
    />
  `,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class FollowingPage {}

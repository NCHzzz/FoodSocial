import { ChangeDetectionStrategy, Component } from '@angular/core';

import { UiPageScaffoldComponent } from '../../shared/ui/page-scaffold/ui-page-scaffold.component';

@Component({
  selector: 'app-notifications-page',
  imports: [UiPageScaffoldComponent],
  template: `
    <app-ui-page-scaffold
      eyebrow="Authenticated route area"
      title="Notifications"
      description="See follows, likes, comments, and repost activity in one place without losing the path back to the food and people involved."
    />
  `,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class NotificationsPage {}

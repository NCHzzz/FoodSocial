import { ChangeDetectionStrategy, Component } from '@angular/core';

import { UiPageScaffoldComponent } from '../../shared/ui/page-scaffold/ui-page-scaffold.component';

@Component({
  selector: 'app-notifications-page',
  imports: [UiPageScaffoldComponent],
  template: `
    <app-ui-page-scaffold
      eyebrow="Authenticated route area"
      title="Notifications"
      description="Notification center access stays gated behind authentication and is now represented as its own lazy route boundary."
    />
  `,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class NotificationsPage {}

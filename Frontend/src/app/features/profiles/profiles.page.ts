import { ChangeDetectionStrategy, Component } from '@angular/core';

import { UiPageScaffoldComponent } from '../../shared/ui/page-scaffold/ui-page-scaffold.component';

@Component({
  selector: 'app-profiles-page',
  imports: [UiPageScaffoldComponent],
  template: `
    <app-ui-page-scaffold
      eyebrow="Public route area"
      title="Profiles"
      description="Browse public profiles, authored food posts, and the identity signals that help you decide whose taste you want to follow."
    />
  `,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ProfilesPage {}

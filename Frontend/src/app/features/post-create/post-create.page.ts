import { ChangeDetectionStrategy, Component } from '@angular/core';

import { UiPageScaffoldComponent } from '../../shared/ui/page-scaffold/ui-page-scaffold.component';

@Component({
  selector: 'app-post-create-page',
  imports: [UiPageScaffoldComponent],
  template: `
    <app-ui-page-scaffold
      eyebrow="Authenticated route area"
      title="Create post"
      description="This route area is reserved for the food-first post composition flow. It now lives behind an authenticated lazy route instead of being implied by root-shell navigation alone."
    />
  `,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class PostCreatePage {}

import { ChangeDetectionStrategy, Component } from '@angular/core';

import { UiPageScaffoldComponent } from '../../shared/ui/page-scaffold/ui-page-scaffold.component';

@Component({
  selector: 'app-post-create-page',
  imports: [UiPageScaffoldComponent],
  template: `
    <app-ui-page-scaffold
      eyebrow="Authenticated route area"
      title="Create post"
      description="Create a food-first post with a clear dish identity, a primary photo, and the place context that helps other people decide what to try."
    />
  `,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class PostCreatePage {}

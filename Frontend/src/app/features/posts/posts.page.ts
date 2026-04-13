import { ChangeDetectionStrategy, Component } from '@angular/core';

import { UiPageScaffoldComponent } from '../../shared/ui/page-scaffold/ui-page-scaffold.component';

@Component({
  selector: 'app-posts-page',
  imports: [UiPageScaffoldComponent],
  template: `
    <app-ui-page-scaffold
      eyebrow="Public route area"
      title="Posts"
      description="Public post browsing stays accessible to guests. This route exists as a lazy feature-area placeholder so the frontend can grow into real feed and post-detail slices without collapsing everything back into the root shell."
    />
  `,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class PostsPage {}

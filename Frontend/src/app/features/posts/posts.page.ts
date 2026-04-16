import { ChangeDetectionStrategy, Component } from '@angular/core';

import { UiPageScaffoldComponent } from '../../shared/ui/page-scaffold/ui-page-scaffold.component';

@Component({
  selector: 'app-posts-page',
  imports: [UiPageScaffoldComponent],
  template: `
    <app-ui-page-scaffold
      eyebrow="Public route area"
      title="Posts"
      description="Explore public food posts, then move from dish curiosity into richer details, saves, and conversation when the content earns it."
    />
  `,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class PostsPage {}

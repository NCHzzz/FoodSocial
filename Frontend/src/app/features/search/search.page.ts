import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { ReactiveFormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';

import { UiButtonDirective } from '../../shared/ui/button/ui-button.directive';
import { UiPageScaffoldComponent } from '../../shared/ui/page-scaffold/ui-page-scaffold.component';
import { UiSurfaceDirective } from '../../shared/ui/surface/ui-surface.directive';
import { PostCardComponent, PostReportSubmission } from '../posts/post-card.component';
import { SearchStateService } from './search-state.service';

@Component({
  selector: 'app-search-page',
  imports: [ReactiveFormsModule, RouterLink, UiButtonDirective, UiPageScaffoldComponent, UiSurfaceDirective, PostCardComponent],
  templateUrl: './search.page.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class SearchPage {
  protected readonly state = inject(SearchStateService);
  protected readonly queryControl = this.state.queryControl;
  protected readonly status = this.state.status;
  protected readonly actionStatus = this.state.actionStatus;
  protected readonly actionMessage = this.state.actionMessage;
  protected readonly dishes = this.state.dishes;
  protected readonly places = this.state.places;
  protected readonly posts = this.state.posts;
  protected readonly assist = this.state.assist;
  protected readonly assistDegraded = this.state.assistDegraded;

  protected canSearch(): boolean {
    return this.state.canSearch();
  }

  protected runSearch(): void {
    this.state.runSearch();
  }

  protected likePost(postId: string): void {
    this.state.likePost(postId);
  }

  protected savePost(postId: string): void {
    this.state.savePost(postId);
  }

  protected repostPost(postId: string): void {
    this.state.repostPost(postId);
  }

  protected saveDish(dishId: string): void {
    this.state.saveDish(dishId);
  }

  protected savePlace(placeId: string): void {
    this.state.savePlace(placeId);
  }

  protected sharePost(postId: string): void {
    void this.state.sharePost(postId);
  }

  protected reportPost(report: PostReportSubmission): void {
    this.state.reportPost(report);
  }
}

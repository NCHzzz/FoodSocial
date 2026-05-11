import { Component } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';

import { PostSummaryResponse } from '../../core/posts/post.models';
import { PostCardComponent } from './post-card.component';

@Component({
  imports: [PostCardComponent],
  template: `
    <app-post-card
      [post]="post"
      (likeRequested)="lastAction = 'like:' + $event"
      (saveRequested)="lastAction = 'save:' + $event"
      (repostRequested)="lastAction = 'repost:' + $event"
      (shareRequested)="lastAction = 'share:' + $event"
      (reportRequested)="lastAction = 'report:' + $event"
    />
  `,
})
class PostCardHostComponent {
  readonly post: PostSummaryResponse = {
    id: 'post-1',
    authorId: 'author-1',
    dishId: 'dish-1',
    placeId: null,
    caption: 'Useful phở note.',
    rating: 4,
    priceRange: '$$',
    tags: ['pho'],
    primaryMediaUrl: null,
    isHidden: false,
    likeCount: 2,
    repostCount: 1,
    commentCount: 3,
    createdAtUtc: '2026-05-04T00:00:00Z',
  };
  lastAction = '';
}

describe('PostCardComponent', () => {
  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [PostCardHostComponent],
      providers: [provideRouter([])],
    }).compileComponents();
  });

  afterEach(() => {
    TestBed.resetTestingModule();
  });

  it('exposes a report action from feed cards', () => {
    const fixture = TestBed.createComponent(PostCardHostComponent);
    fixture.detectChanges();

    const reportButton = Array.from((fixture.nativeElement as HTMLElement).querySelectorAll('button'))
      .find((button) => button.textContent?.includes('Report'));

    expect(reportButton).toBeTruthy();
    reportButton?.click();

    expect(fixture.componentInstance.lastAction).toBe('report:post-1');
  });
});

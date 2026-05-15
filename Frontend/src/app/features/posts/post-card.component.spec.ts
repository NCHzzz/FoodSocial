import { Component } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { By } from '@angular/platform-browser';
import { provideRouter } from '@angular/router';
import { of } from 'rxjs';

import { DishApi } from '../../core/dishes/infrastructure/dish.api';
import { PlaceApi } from '../../core/places/infrastructure/place.api';
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
      (reportRequested)="lastReport = $event"
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
  lastReport: unknown = null;
}

describe('PostCardComponent', () => {
  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [PostCardHostComponent],
      providers: [
        provideRouter([]),
        {
          provide: DishApi,
          useValue: {
            getDish: () => of({
              id: 'dish-1',
              name: 'Phở bò',
              normalizedName: 'pho bo',
              description: null,
              category: 'Noodle soup',
              cuisineType: 'Vietnamese',
              aliases: [],
              ingredients: [],
              dietaryFlags: [],
              status: 'Active',
              createdAtUtc: '2026-05-04T00:00:00Z',
              updatedAtUtc: '2026-05-04T00:00:00Z',
              deprecatedAtUtc: null,
            }),
          },
        },
        { provide: PlaceApi, useValue: { getPlace: () => of({}) } },
      ],
    }).compileComponents();
  });

  afterEach(() => {
    TestBed.resetTestingModule();
  });

  it('renders resolved dish names instead of raw dish identifiers', async () => {
    const fixture = TestBed.createComponent(PostCardHostComponent);
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();

    const text = (fixture.nativeElement as HTMLElement).textContent ?? '';

    expect(text).toContain('Phở bò');
    expect(text).not.toContain('Dish dish-1');
  });

  it('collects report reason and note before emitting report requests', () => {
    const fixture = TestBed.createComponent(PostCardHostComponent);
    fixture.detectChanges();

    const card = fixture.debugElement.query(By.directive(PostCardComponent)).componentInstance as PostCardComponent;
    card['reportReasonControl'].setValue('incorrect_food_information');
    card['reportDescriptionControl'].setValue('Menu claim looks wrong.');
    card['submitReport']();

    expect(fixture.componentInstance.lastReport).toEqual({
      postId: 'post-1',
      reason: 'incorrect_food_information',
      description: 'Menu claim looks wrong.',
    });
  });
});

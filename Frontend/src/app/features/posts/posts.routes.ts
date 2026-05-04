import { Routes } from '@angular/router';

export const POSTS_ROUTES: Routes = [
  {
    path: '',
    loadComponent: () => import('./posts.page').then((module) => module.PostsPage),
  },
  {
    path: ':postId',
    loadComponent: () => import('./post-detail.page').then((module) => module.PostDetailPage),
  },
];

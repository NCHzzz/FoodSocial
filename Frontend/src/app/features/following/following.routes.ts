import { Routes } from '@angular/router';

export const FOLLOWING_ROUTES: Routes = [
  {
    path: '',
    loadComponent: () => import('./following.page').then((module) => module.FollowingPage),
  },
];

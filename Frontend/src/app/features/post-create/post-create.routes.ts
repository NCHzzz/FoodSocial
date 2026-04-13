import { Routes } from '@angular/router';

export const POST_CREATE_ROUTES: Routes = [
  {
    path: '',
    loadComponent: () => import('./post-create.page').then((module) => module.PostCreatePage),
  },
];

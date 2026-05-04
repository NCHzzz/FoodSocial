import { Routes } from '@angular/router';

export const MODERATION_ROUTES: Routes = [
  {
    path: '',
    loadComponent: () => import('./moderation.page').then((module) => module.ModerationPage),
  },
];

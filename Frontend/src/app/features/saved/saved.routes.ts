import { Routes } from '@angular/router';

export const SAVED_ROUTES: Routes = [
  {
    path: '',
    loadComponent: () => import('./saved.page').then((module) => module.SavedPage),
  },
];

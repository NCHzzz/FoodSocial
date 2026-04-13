import { Routes } from '@angular/router';

export const DISCOVER_ROUTES: Routes = [
  {
    path: '',
    loadComponent: () => import('./discover.page').then((module) => module.DiscoverPage),
  },
];

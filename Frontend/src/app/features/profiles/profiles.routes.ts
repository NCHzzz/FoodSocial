import { Routes } from '@angular/router';

export const PROFILES_ROUTES: Routes = [
  {
    path: '',
    loadComponent: () => import('./profiles.page').then((module) => module.ProfilesPage),
  },
  {
    path: ':identityId',
    loadComponent: () => import('./public-profile.page').then((module) => module.PublicProfilePage),
  },
];

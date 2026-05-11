import { Routes } from '@angular/router';

import { ProfileStateService } from './profile-state.service';

export const PROFILE_ROUTES: Routes = [
  {
    path: '',
    loadComponent: () => import('./profile.page').then((module) => module.ProfilePage),
    providers: [ProfileStateService],
  },
];

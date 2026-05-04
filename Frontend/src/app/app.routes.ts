import { Routes } from '@angular/router';

import { authenticatedRouteGuard } from './core/guards/auth.guard';
import { AppShellComponent } from './shell/app-shell.component';

export const routes: Routes = [
  {
    path: '',
    component: AppShellComponent,
    children: [
      {
        path: '',
        pathMatch: 'full',
        redirectTo: 'discover',
      },
      {
        path: 'discover',
        loadChildren: () => import('./features/discover/discover.routes').then((module) => module.DISCOVER_ROUTES),
      },
      {
        path: 'search',
        loadChildren: () => import('./features/search/search.routes').then((module) => module.SEARCH_ROUTES),
      },
      {
        path: 'posts',
        loadChildren: () => import('./features/posts/posts.routes').then((module) => module.POSTS_ROUTES),
      },
      {
        path: 'profiles',
        loadChildren: () => import('./features/profiles/profiles.routes').then((module) => module.PROFILES_ROUTES),
      },
      {
        path: 'following',
        canActivate: [authenticatedRouteGuard],
        loadChildren: () => import('./features/following/following.routes').then((module) => module.FOLLOWING_ROUTES),
      },
      {
        path: 'post-create',
        canActivate: [authenticatedRouteGuard],
        loadChildren: () => import('./features/post-create/post-create.routes').then((module) => module.POST_CREATE_ROUTES),
      },
      {
        path: 'moderation',
        canActivate: [authenticatedRouteGuard],
        loadChildren: () => import('./features/moderation/moderation.routes').then((module) => module.MODERATION_ROUTES),
      },
      {
        path: 'notifications',
        canActivate: [authenticatedRouteGuard],
        loadChildren: () => import('./features/notifications/notifications.routes').then((module) => module.NOTIFICATIONS_ROUTES),
      },
      {
        path: 'profile',
        canActivate: [authenticatedRouteGuard],
        loadChildren: () => import('./features/profile/profile.routes').then((module) => module.PROFILE_ROUTES),
      },
    ],
  },
  {
    path: '**',
    redirectTo: 'discover',
  },
];

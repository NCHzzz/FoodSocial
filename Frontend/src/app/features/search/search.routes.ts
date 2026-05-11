import { Routes } from '@angular/router';

import { SearchStateService } from './search-state.service';

export const SEARCH_ROUTES: Routes = [
  {
    path: '',
    loadComponent: () => import('./search.page').then((module) => module.SearchPage),
    providers: [SearchStateService],
  },
];

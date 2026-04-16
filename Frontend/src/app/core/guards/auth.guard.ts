import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';

import { AuthSessionService } from '../auth/auth-session.service';

export const authenticatedRouteGuard: CanActivateFn = async () => {
  const authSessionService = inject(AuthSessionService);
  const router = inject(Router);
  const currentState = await authSessionService.ensureSession();

  return currentState.loadStatus === 'authenticated' ? true : router.createUrlTree(['/discover']);
};

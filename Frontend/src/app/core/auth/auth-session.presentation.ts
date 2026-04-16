import { AuthSessionState } from './auth-session.models';

const AUTHENTICATED_SESSION_BADGE_CLASSES = 'bg-emerald-100 text-emerald-700';
const UNAVAILABLE_SESSION_BADGE_CLASSES = 'bg-rose-100 text-rose-700';
const LOADING_SESSION_BADGE_CLASSES = 'bg-amber-100 text-amber-700';
const GUEST_SESSION_BADGE_CLASSES = 'bg-slate-200 text-slate-700';

export function isAuthenticatedSession(state: AuthSessionState): boolean {
  return state.loadStatus === 'authenticated' && state.session !== null;
}

export function getAuthSessionBadgeClasses(state: AuthSessionState): string {
  if (isAuthenticatedSession(state)) {
    return AUTHENTICATED_SESSION_BADGE_CLASSES;
  }

  if (state.loadStatus === 'unavailable') {
    return UNAVAILABLE_SESSION_BADGE_CLASSES;
  }

  if (state.loadStatus === 'loading') {
    return LOADING_SESSION_BADGE_CLASSES;
  }

  return GUEST_SESSION_BADGE_CLASSES;
}

export function getAuthSessionBadgeLabel(state: AuthSessionState): string {
  if (isAuthenticatedSession(state)) {
    return 'Authenticated';
  }

  if (state.loadStatus === 'loading') {
    return 'Checking';
  }

  if (state.loadStatus === 'unavailable') {
    return 'Unavailable';
  }

  return 'Guest';
}

export function getAuthSessionSummary(state: AuthSessionState): string {
  const session = state.session;

  if (state.loadStatus === 'authenticated' && session !== null) {
    return session.identity.bootstrapPending
      ? 'Your FoodSocial account is still finishing setup.'
      : 'Your FoodSocial session is active.';
  }

  if (state.loadStatus === 'loading') {
    return 'Checking whether you already have an active session.';
  }

  if (state.loadStatus === 'unavailable') {
    return 'Session status is temporarily unavailable. Public browsing still works while protected actions stay gated.';
  }

  return 'Browse public food content now, then sign in when you want deeper social actions.';
}

export function getDiscoverSessionAvailabilityMessage(state: AuthSessionState): string {
  if (isAuthenticatedSession(state)) {
    return 'Signed in through the verified BFF browser flow.';
  }

  if (state.loadStatus === 'loading') {
    return 'Verifying whether a trusted browser session already exists.';
  }

  if (state.loadStatus === 'unavailable') {
    return 'Session status is temporarily unavailable, so protected actions stay gated until the auth edge recovers.';
  }

  return 'Guest browsing remains available while deeper actions stay gated.';
}

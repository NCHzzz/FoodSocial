export interface AuthenticatedUserDto {
  subject: string;
  preferredUsername: string | null;
  email: string | null;
  roles: string[];
}

export interface SessionIdentityDto {
  identityId: string;
  state: string;
  bootstrapPending: boolean;
  downstreamBootstrapPending: boolean;
}

export interface CurrentUserResponse {
  user: AuthenticatedUserDto;
  identity: SessionIdentityDto;
  serverTimeUtc: string;
}

export type AuthSessionLoadStatus = 'loading' | 'guest' | 'authenticated' | 'unavailable';

export interface AuthSessionState {
  loadStatus: AuthSessionLoadStatus;
  session: CurrentUserResponse | null;
  isLoggingOut: boolean;
  lastLoadFailed: boolean;
}

export const createLoadingAuthSessionState = (): AuthSessionState => ({
  loadStatus: 'loading',
  session: null,
  isLoggingOut: false,
  lastLoadFailed: false,
});

export const createGuestAuthSessionState = (): AuthSessionState => ({
  loadStatus: 'guest',
  session: null,
  isLoggingOut: false,
  lastLoadFailed: false,
});

export const createUnavailableAuthSessionState = (): AuthSessionState => ({
  loadStatus: 'unavailable',
  session: null,
  isLoggingOut: false,
  lastLoadFailed: true,
});

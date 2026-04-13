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

export type AuthSessionStatus = 'loading' | 'guest' | 'authenticated';

export interface AuthSessionState {
  status: AuthSessionStatus;
  session: CurrentUserResponse | null;
  isLoggingOut: boolean;
}

export const createLoadingAuthSessionState = (): AuthSessionState => ({
  status: 'loading',
  session: null,
  isLoggingOut: false,
});

export const createGuestAuthSessionState = (): AuthSessionState => ({
  status: 'guest',
  session: null,
  isLoggingOut: false,
});

export interface AppShellNavigationItem {
  label: string;
  route: string;
}

export const GUEST_NAVIGATION_ITEMS: readonly AppShellNavigationItem[] = [
  { label: 'Discover', route: '/discover' },
  { label: 'Posts', route: '/posts' },
  { label: 'Profiles', route: '/profiles' },
];

export const AUTHENTICATED_NAVIGATION_ITEMS: readonly AppShellNavigationItem[] = [
  { label: 'Discover', route: '/discover' },
  { label: 'Following', route: '/following' },
  { label: 'Create', route: '/post-create' },
  { label: 'Notifications', route: '/notifications' },
  { label: 'Profile', route: '/profile' },
];

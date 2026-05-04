import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { of } from 'rxjs';
import { vi } from 'vitest';

import { AuthSessionService } from '../../core/auth/auth-session.service';
import { AuthSessionState } from '../../core/auth/auth-session.models';
import { PostApi } from '../../core/posts/infrastructure/post.api';
import { NotificationListResponse, NotificationResponse } from '../../core/posts/post.models';
import { UserProfileApi } from '../../core/user-profiles/infrastructure/user-profile.api';
import { UserNotificationListResponse, UserNotificationResponse } from '../../core/user-profiles/user-profile.models';
import { NotificationsPage } from './notifications.page';

describe('NotificationsPage', () => {
  const identityId = '99999999-9999-9999-9999-999999999999';
  const authenticatedState: AuthSessionState = {
    loadStatus: 'authenticated',
    session: {
      user: {
        subject: 'profile-subject',
        preferredUsername: 'profile-user',
        email: 'profile@example.test',
        roles: ['user'],
      },
      identity: {
        identityId,
        state: 'Active',
        bootstrapPending: false,
        downstreamBootstrapPending: false,
      },
      serverTimeUtc: '2026-05-05T00:00:00Z',
    },
    isLoggingOut: false,
    lastLoadFailed: false,
  };
  const notification: NotificationResponse = {
    id: '11111111-1111-1111-1111-111111111111',
    recipientId: '22222222-2222-2222-2222-222222222222',
    actorId: '33333333-3333-3333-3333-333333333333',
    type: 'PostReposted',
    targetType: 'Repost',
    targetId: '44444444-4444-4444-4444-444444444444',
    postId: '55555555-5555-5555-5555-555555555555',
    message: 'reposted your food post',
    isRead: false,
    createdAtUtc: '2026-05-05T00:00:00Z',
    readAtUtc: null,
  };
  const userNotification: UserNotificationResponse = {
    id: '66666666-6666-6666-6666-666666666666',
    recipientIdentityId: identityId,
    actorIdentityId: '77777777-7777-7777-7777-777777777777',
    type: 'UserFollowed',
    message: 'followed you',
    isRead: false,
    createdAtUtc: '2026-05-05T02:00:00Z',
    readAtUtc: null,
  };
  const notificationsResponse: NotificationListResponse = {
    items: [notification],
    limit: 20,
    unreadCount: 1,
  };
  const userNotificationsResponse: UserNotificationListResponse = {
    items: [userNotification],
    limit: 50,
    unreadCount: 1,
  };
  let authSessionMock: { ensureSession: ReturnType<typeof vi.fn> };
  let postApiMock: { getNotifications: ReturnType<typeof vi.fn>; markNotificationRead: ReturnType<typeof vi.fn> };
  let userProfileApiMock: { getNotifications: ReturnType<typeof vi.fn>; markNotificationRead: ReturnType<typeof vi.fn> };

  beforeEach(async () => {
    authSessionMock = {
      ensureSession: vi.fn(() => Promise.resolve(authenticatedState)),
    };
    postApiMock = {
      getNotifications: vi.fn((_limit: number) => of(notificationsResponse)),
      markNotificationRead: vi.fn((notificationId: string) => of({ ...notification, id: notificationId, isRead: true, readAtUtc: '2026-05-05T01:00:00Z' })),
    };
    userProfileApiMock = {
      getNotifications: vi.fn((_identityId: string) => of(userNotificationsResponse)),
      markNotificationRead: vi.fn((_identityId: string, notificationId: string) => of({ ...userNotification, id: notificationId, isRead: true, readAtUtc: '2026-05-05T03:00:00Z' })),
    };

    await TestBed.configureTestingModule({
      imports: [NotificationsPage],
      providers: [
        provideRouter([]),
        { provide: AuthSessionService, useValue: authSessionMock },
        { provide: PostApi, useValue: postApiMock },
        { provide: UserProfileApi, useValue: userProfileApiMock },
      ],
    }).compileComponents();
  });

  afterEach(() => {
    TestBed.resetTestingModule();
  });

  it('renders notification stream and marks notifications read', async () => {
    const fixture = TestBed.createComponent(NotificationsPage);
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();

    const element = fixture.nativeElement as HTMLElement;
    expect(authSessionMock.ensureSession).toHaveBeenCalled();
    expect(postApiMock.getNotifications).toHaveBeenCalledWith(20);
    expect(userProfileApiMock.getNotifications).toHaveBeenCalledWith(identityId);
    expect(element.textContent).toContain('Food and profile activity are live');
    expect(element.textContent).toContain('reposted your food post');
    expect(element.textContent).toContain('followed you');
    expect(element.textContent).toContain('2');

    const component = fixture.componentInstance as unknown as { markRead(notification: unknown): void };
    component.markRead({
      id: notification.id,
      source: 'post',
      sourceLabel: 'Post',
      actorId: notification.actorId,
      type: notification.type,
      message: notification.message,
      isRead: notification.isRead,
      createdAtUtc: notification.createdAtUtc,
      readAtUtc: notification.readAtUtc,
      postId: notification.postId,
    });
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();

    expect(postApiMock.markNotificationRead).toHaveBeenCalledWith(notification.id);
    expect(element.textContent).toContain('Notification marked read.');
    expect(element.textContent).toContain('Read');
  });

  it('marks profile notifications read through the UserProfile facade', async () => {
    const fixture = TestBed.createComponent(NotificationsPage);
    fixture.detectChanges();
    await fixture.whenStable();

    const component = fixture.componentInstance as unknown as { markRead(notification: unknown): void };
    component.markRead({
      id: userNotification.id,
      source: 'user',
      sourceLabel: 'Profile',
      actorId: userNotification.actorIdentityId,
      type: userNotification.type,
      message: userNotification.message,
      isRead: userNotification.isRead,
      createdAtUtc: userNotification.createdAtUtc,
      readAtUtc: userNotification.readAtUtc,
      postId: null,
    });
    await fixture.whenStable();

    expect(userProfileApiMock.markNotificationRead).toHaveBeenCalledWith(identityId, userNotification.id);
  });
});

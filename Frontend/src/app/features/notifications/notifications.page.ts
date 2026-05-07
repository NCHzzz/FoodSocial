import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { Observable, catchError, forkJoin, from, map, of, switchMap } from 'rxjs';

import { AuthSessionService } from '../../core/auth/auth-session.service';
import { PostApi } from '../../core/posts/infrastructure/post.api';
import { NotificationResponse } from '../../core/posts/post.models';
import { UserProfileApi } from '../../core/user-profiles/infrastructure/user-profile.api';
import { UserNotificationResponse } from '../../core/user-profiles/user-profile.models';
import { UiButtonDirective } from '../../shared/ui/button/ui-button.directive';
import { UiPageScaffoldComponent } from '../../shared/ui/page-scaffold/ui-page-scaffold.component';
import { UiSurfaceDirective } from '../../shared/ui/surface/ui-surface.directive';

type NotificationsStatus = 'loading' | 'loaded' | 'empty' | 'error';
type NotificationsActionStatus = 'idle' | 'pending' | 'success' | 'error';
type NotificationSource = 'post' | 'user';

interface NotificationItem {
  id: string;
  source: NotificationSource;
  sourceLabel: string;
  actorId: string;
  type: string;
  message: string;
  isRead: boolean;
  createdAtUtc: string;
  readAtUtc: string | null;
  postId: string | null;
}

@Component({
  selector: 'app-notifications-page',
  imports: [RouterLink, UiButtonDirective, UiPageScaffoldComponent, UiSurfaceDirective],
  template: `
      <app-ui-page-scaffold
      eyebrow="Notification center"
      title="Notifications"
      description="See food-post activity and follow notifications in one MVP inbox while a dedicated notification service remains future extraction work."
    >
      <div class="mt-8 grid gap-4 lg:grid-cols-[minmax(0,1fr)_18rem]">
        <section appUiSurface="soft" appUiSurfacePadding="comfortable">
          <p class="text-xs font-semibold uppercase tracking-[0.22em] text-[#c96442]">Activity stream</p>
          <h2 class="mt-2 text-xl font-semibold tracking-tight text-[#141413]">Food and profile activity are live</h2>
          <p class="mt-3 max-w-2xl text-sm leading-6 text-[#5e5d59]">
            This MVP stream combines Post.Api interactions and UserProfile follow notices through the BFF. It stays bounded until a dedicated Notification service is extracted.
          </p>
          <div class="mt-5 flex flex-wrap items-center gap-3">
            <button type="button" appUiButton="secondary" (click)="loadNotifications()" [disabled]="status() === 'loading'">Refresh</button>
            <span class="rounded-full bg-[#f0eee6] px-3 py-2 text-sm font-semibold text-[#c96442]">{{ unreadCount() }} unread</span>
          </div>
        </section>

        <aside appUiSurface="default" appUiSurfacePadding="comfortable">
          <p class="text-sm font-semibold text-[#141413]">Current scope</p>
          <ul class="mt-3 space-y-3 text-sm leading-6 text-[#5e5d59]">
            <li>Likes on your food posts.</li>
            <li>Comments on your food posts.</li>
            <li>Reposts of your food posts.</li>
            <li>New followers on your profile.</li>
          </ul>
        </aside>
      </div>

      @if (actionStatus() === 'success') {
        <p class="mt-5 rounded-2xl border border-emerald-200 bg-emerald-50 px-4 py-3 text-sm font-semibold text-emerald-800" [textContent]="actionMessage()"></p>
      }
      @if (actionStatus() === 'error') {
        <p class="mt-5 rounded-2xl border border-red-200 bg-red-50 px-4 py-3 text-sm font-semibold text-red-700" [textContent]="actionMessage()"></p>
      }
      @if (sourceFailureMessages().length > 0) {
        <div class="mt-5 grid gap-2">
          @for (message of sourceFailureMessages(); track message) {
            <p class="rounded-2xl border border-amber-200 bg-amber-50 px-4 py-3 text-sm font-semibold text-amber-900" [textContent]="message"></p>
          }
        </div>
      }

      <section class="mt-8">
        @if (status() === 'loading') {
          <div appUiSurface="soft" appUiSurfacePadding="comfortable">
            <p class="text-sm font-semibold text-[#141413]">Loading notifications</p>
            <p class="mt-2 text-sm text-[#5e5d59]">Reading your latest food and profile activity.</p>
          </div>
        }

        @if (status() === 'error') {
          <div appUiSurface="soft" appUiSurfacePadding="comfortable">
            <p class="text-sm font-semibold text-[#141413]">Notifications unavailable</p>
            <p class="mt-2 text-sm text-[#5e5d59]">Retry when the BFF notification facade is reachable.</p>
          </div>
        }

        @if (status() === 'empty') {
          <div appUiSurface="soft" appUiSurfacePadding="comfortable">
            <p class="text-sm font-semibold text-[#141413]">No activity yet</p>
            <p class="mt-2 text-sm text-[#5e5d59]">Likes, comments, reposts, and new followers will appear here.</p>
            <a class="mt-4" appUiButton="accent" routerLink="/posts">Browse posts</a>
          </div>
        }

        @if (status() === 'loaded') {
          <div class="grid gap-3">
            @for (notification of notifications(); track trackNotification(notification)) {
              <article appUiSurface="default" appUiSurfacePadding="comfortable">
                <div class="flex flex-col gap-4 sm:flex-row sm:items-start sm:justify-between">
                  <div>
                    <p class="text-xs font-semibold uppercase tracking-[0.22em] text-[#c96442]">{{ notification.sourceLabel }} · {{ notification.type }}</p>
                    <h3 class="mt-2 text-base font-semibold text-[#141413]" [textContent]="notification.message"></h3>
                    <p class="mt-2 break-all text-sm leading-6 text-[#5e5d59]">
                      Actor {{ notification.actorId }} · {{ formatDate(notification.createdAtUtc) }}
                    </p>
                    @if (notification.postId) {
                      <a class="mt-3 inline-flex text-sm font-semibold text-[#c96442] hover:text-[#4d4c48]" [routerLink]="['/posts', notification.postId]">Open post</a>
                    }
                  </div>
                  @if (!notification.isRead) {
                    <button type="button" appUiButton="secondary" (click)="markRead(notification)" [disabled]="actionStatus() === 'pending'">Mark read</button>
                  } @else {
                    <span class="rounded-full bg-emerald-50 px-3 py-2 text-xs font-semibold text-emerald-700">Read</span>
                  }
                </div>
              </article>
            }
          </div>
        }
      </section>
    </app-ui-page-scaffold>
  `,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class NotificationsPage {
  private readonly authSession = inject(AuthSessionService);
  private readonly postApi = inject(PostApi);
  private readonly userProfileApi = inject(UserProfileApi);

  protected readonly status = signal<NotificationsStatus>('loading');
  protected readonly actionStatus = signal<NotificationsActionStatus>('idle');
  protected readonly actionMessage = signal('');
  protected readonly notifications = signal<NotificationItem[]>([]);
  protected readonly unreadCount = signal(0);
  protected readonly sourceFailureMessages = signal<string[]>([]);
  private readonly currentIdentityId = signal<string | null>(null);

  constructor() {
    this.loadNotifications();
  }

  protected loadNotifications(): void {
    this.status.set('loading');
    this.actionStatus.set('idle');
    this.sourceFailureMessages.set([]);
    from(this.authSession.ensureSession()).pipe(
      switchMap((sessionState) => {
        const identityId = sessionState.session?.identity.identityId ?? null;
        this.currentIdentityId.set(identityId);
        if (sessionState.loadStatus !== 'authenticated' || identityId === null) {
          return of({ postItems: null, userItems: null });
        }

        return forkJoin({
          postItems: this.postApi.getNotifications(20).pipe(
            map((response) => response.items.map((item) => this.mapPostNotification(item))),
            catchError(() => of(null)),
          ),
          userItems: this.userProfileApi.getNotifications(identityId).pipe(
            map((response) => response.items.map((item) => this.mapUserNotification(item))),
            catchError(() => of(null)),
          ),
        });
      }),
    ).subscribe({
      next: ({ postItems, userItems }) => {
        if (postItems === null && userItems === null) {
          this.notifications.set([]);
          this.unreadCount.set(0);
          this.sourceFailureMessages.set([]);
          this.status.set('error');
          return;
        }

        const sourceFailures = [
          postItems === null ? 'Post notifications unavailable. Showing any follow activity that is still reachable.' : null,
          userItems === null ? 'Follow notifications unavailable. Showing any post activity that is still reachable.' : null,
        ].filter((message): message is string => message !== null);
        const items = [...(postItems ?? []), ...(userItems ?? [])]
          .sort((left, right) => Date.parse(right.createdAtUtc) - Date.parse(left.createdAtUtc));
        this.notifications.set(items);
        this.sourceFailureMessages.set(sourceFailures);
        this.updateUnreadCount(items);
        this.status.set(items.length > 0 ? 'loaded' : 'empty');
      },
      error: () => {
        this.notifications.set([]);
        this.unreadCount.set(0);
        this.sourceFailureMessages.set([]);
        this.status.set('error');
      },
    });
  }

  protected markRead(notification: NotificationItem): void {
    this.actionStatus.set('pending');
    const currentIdentityId = this.currentIdentityId();
    const request: Observable<NotificationItem | null> = notification.source === 'post'
      ? this.postApi.markNotificationRead(notification.id).pipe(map((item) => this.mapPostNotification(item)))
      : currentIdentityId === null
        ? of(null)
        : this.userProfileApi.markNotificationRead(currentIdentityId, notification.id).pipe(map((item) => this.mapUserNotification(item)));

    request.subscribe({
      next: (updatedNotification) => {
        if (updatedNotification === null) {
          this.actionStatus.set('error');
          this.actionMessage.set('Notification update failed. Sign in again and retry.');
          return;
        }

        const nextItems = this.notifications().map((item) =>
          item.source === updatedNotification.source && item.id === updatedNotification.id ? updatedNotification : item);
        this.notifications.set(nextItems);
        this.updateUnreadCount(nextItems);
        this.actionStatus.set('success');
        this.actionMessage.set('Notification marked read.');
      },
      error: () => {
        this.actionStatus.set('error');
        this.actionMessage.set('Notification update failed. Retry when the BFF is available.');
      },
    });
  }

  protected formatDate(value: string): string {
    return new Date(value).toLocaleString();
  }

  protected trackNotification(notification: NotificationItem): string {
    return `${notification.source}:${notification.id}`;
  }

  private updateUnreadCount(items: NotificationItem[]): void {
    this.unreadCount.set(items.filter((item) => !item.isRead).length);
  }

  private mapPostNotification(notification: NotificationResponse): NotificationItem {
    return {
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
    };
  }

  private mapUserNotification(notification: UserNotificationResponse): NotificationItem {
    return {
      id: notification.id,
      source: 'user',
      sourceLabel: 'Profile',
      actorId: notification.actorIdentityId,
      type: notification.type,
      message: notification.message,
      isRead: notification.isRead,
      createdAtUtc: notification.createdAtUtc,
      readAtUtc: notification.readAtUtc,
      postId: null,
    };
  }
}

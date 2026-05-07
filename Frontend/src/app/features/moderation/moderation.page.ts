import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';

import { AuthSessionService } from '../../core/auth/auth-session.service';
import { isAuthenticatedSession } from '../../core/auth/auth-session.presentation';
import { PostApi } from '../../core/posts/infrastructure/post.api';
import { ContentReportResponse } from '../../core/posts/post.models';
import { UiButtonDirective } from '../../shared/ui/button/ui-button.directive';
import { UiPageScaffoldComponent } from '../../shared/ui/page-scaffold/ui-page-scaffold.component';
import { UiSurfaceDirective } from '../../shared/ui/surface/ui-surface.directive';

type ModerationStatus = 'loading' | 'loaded' | 'empty' | 'forbidden' | 'error';
type ModerationActionStatus = 'idle' | 'pending' | 'success' | 'error';

@Component({
  selector: 'app-moderation-page',
  imports: [RouterLink, UiButtonDirective, UiPageScaffoldComponent, UiSurfaceDirective],
  template: `
    <app-ui-page-scaffold
      eyebrow="Manual-review-first moderation"
      title="Moderation queue"
      description="Review submitted post and comment reports without auto-hiding content. Moderator actions stay explicit and audited."
    >
      <section class="mt-8 grid gap-4 lg:grid-cols-[minmax(0,1fr)_18rem]">
        <div appUiSurface="soft" appUiSurfacePadding="comfortable">
          <p class="text-xs font-semibold uppercase tracking-[0.22em] text-[#c96442]">Review policy</p>
          <h2 class="mt-2 text-xl font-semibold tracking-tight text-[#141413]">Manual queue for reported content</h2>
          <p class="mt-3 max-w-2xl text-sm leading-6 text-[#5e5d59]">
            MVP moderation stays human-reviewed. Reports can be dismissed or resolved with a content action through the BFF-backed Post moderation facade.
          </p>
          <button class="mt-5" type="button" appUiButton="secondary" (click)="loadReports()" [disabled]="status() === 'loading'">Refresh queue</button>
        </div>

        <aside appUiSurface="default" appUiSurfacePadding="comfortable">
          <p class="text-sm font-semibold text-[#141413]">Access</p>
          <p class="mt-3 text-sm leading-6 text-[#5e5d59]">
            Moderator or admin role required. Non-moderator sessions stay blocked client-side and again by the BFF/Post.Api trust boundary.
          </p>
          <a class="mt-5" appUiButton="secondary" routerLink="/discover">Back to Discover</a>
        </aside>
      </section>

      @if (actionStatus() === 'success') {
        <p class="mt-5 rounded-2xl border border-emerald-200 bg-emerald-50 px-4 py-3 text-sm font-semibold text-emerald-800" [textContent]="actionMessage()"></p>
      }
      @if (actionStatus() === 'error') {
        <p class="mt-5 rounded-2xl border border-red-200 bg-red-50 px-4 py-3 text-sm font-semibold text-red-700" [textContent]="actionMessage()"></p>
      }

      <section class="mt-8">
        @if (status() === 'forbidden') {
          <div appUiSurface="soft" appUiSurfacePadding="comfortable">
            <p class="text-sm font-semibold text-[#141413]">Moderator access required</p>
            <p class="mt-2 text-sm text-[#5e5d59]">Sign in with a moderator or admin role to review submitted reports.</p>
          </div>
        }

        @if (status() === 'loading') {
          <div appUiSurface="soft" appUiSurfacePadding="comfortable">
            <p class="text-sm font-semibold text-[#141413]">Loading reports</p>
            <p class="mt-2 text-sm text-[#5e5d59]">Reading the latest manual-review queue from the BFF.</p>
          </div>
        }

        @if (status() === 'error') {
          <div appUiSurface="soft" appUiSurfacePadding="comfortable">
            <p class="text-sm font-semibold text-[#141413]">Moderation queue unavailable</p>
            <p class="mt-2 text-sm text-[#5e5d59]">Retry when the Post moderation facade is reachable.</p>
          </div>
        }

        @if (status() === 'empty') {
          <div appUiSurface="soft" appUiSurfacePadding="comfortable">
            <p class="text-sm font-semibold text-[#141413]">No reports waiting</p>
            <p class="mt-2 text-sm text-[#5e5d59]">The manual review queue is empty.</p>
          </div>
        }

        @if (status() === 'loaded') {
          <div class="grid gap-4">
            @for (report of reports(); track report.id) {
              <article appUiSurface="default" appUiSurfacePadding="comfortable">
                <div class="flex flex-col gap-4 lg:flex-row lg:items-start lg:justify-between">
                  <div>
                    <p class="text-xs font-semibold uppercase tracking-[0.22em] text-[#c96442]">{{ report.targetType }} report</p>
                    <h3 class="mt-2 text-lg font-semibold tracking-tight text-[#141413]" [textContent]="report.reason"></h3>
                    <p class="mt-2 text-sm leading-6 text-[#5e5d59]" [textContent]="report.description || 'No reporter description.'"></p>
                    <dl class="mt-4 grid gap-2 text-sm text-[#5e5d59] sm:grid-cols-2">
                      <div>
                        <dt class="font-semibold text-[#141413]">Target</dt>
                        <dd [textContent]="report.targetId"></dd>
                      </div>
                      <div>
                        <dt class="font-semibold text-[#141413]">Reporter</dt>
                        <dd [textContent]="report.reporterId"></dd>
                      </div>
                      <div>
                        <dt class="font-semibold text-[#141413]">Status</dt>
                        <dd [textContent]="report.status"></dd>
                      </div>
                      <div>
                        <dt class="font-semibold text-[#141413]">Submitted</dt>
                        <dd [textContent]="formatDate(report.createdAtUtc)"></dd>
                      </div>
                    </dl>
                  </div>

                  @if (report.status === 'Submitted') {
                    <div class="flex shrink-0 flex-wrap gap-2">
                      <button type="button" appUiButton="secondary" (click)="resolveReport(report.id, 'dismiss')" [disabled]="actionStatus() === 'pending'">Dismiss</button>
                      <button type="button" appUiButton="accent" (click)="resolveReport(report.id, 'hide')" [disabled]="actionStatus() === 'pending'">Hide content</button>
                    </div>
                  } @else {
                    <p class="rounded-full bg-emerald-50 px-3 py-1 text-xs font-semibold text-emerald-700">Resolved</p>
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
export class ModerationPage {
  private readonly postApi = inject(PostApi);
  private readonly authSessionService = inject(AuthSessionService);

  protected readonly authState = this.authSessionService.authState;
  protected readonly isAuthenticated = computed(() => isAuthenticatedSession(this.authState()));
  protected readonly signedInIdentityId = computed(() => this.authState().session?.identity.identityId ?? '00000000-0000-0000-0000-000000000000');
  protected readonly isModerator = computed(() => this.hasModerationRole(this.authState().session?.user.roles ?? []));
  protected readonly status = signal<ModerationStatus>('loading');
  protected readonly actionStatus = signal<ModerationActionStatus>('idle');
  protected readonly actionMessage = signal('');
  protected readonly reports = signal<ContentReportResponse[]>([]);

  constructor() {
    this.loadReports();
  }

  protected loadReports(): void {
    if (!this.isAuthenticated() || !this.isModerator()) {
      this.reports.set([]);
      this.status.set('forbidden');
      return;
    }

    this.status.set('loading');
    this.postApi.getAdminReports(20).subscribe({
      next: (response) => {
        this.reports.set(response.items);
        this.status.set(response.items.length > 0 ? 'loaded' : 'empty');
      },
      error: () => {
        this.reports.set([]);
        this.status.set('error');
      },
    });
  }

  protected resolveReport(reportId: string, action: 'dismiss' | 'hide'): void {
    this.actionStatus.set('pending');
    this.postApi.resolveReport(reportId, {
      moderatorId: this.signedInIdentityId(),
      action,
      auditNote: action === 'hide' ? 'Moderator hid reported content from the MVP queue.' : 'Moderator dismissed report from the MVP queue.',
    }).subscribe({
      next: (report) => {
        this.reports.update((reports) => reports.map((item) => item.id === report.id ? report : item));
        this.actionStatus.set('success');
        this.actionMessage.set(action === 'hide' ? 'Report resolved and content hidden.' : 'Report dismissed.');
      },
      error: () => {
        this.actionStatus.set('error');
        this.actionMessage.set('Report resolution failed. Retry when the moderation facade is available.');
      },
    });
  }

  protected formatDate(value: string): string {
    return new Date(value).toLocaleString();
  }

  private hasModerationRole(roles: string[]): boolean {
    return roles.some((role) => role.toLowerCase() === 'moderator' || role.toLowerCase() === 'admin');
  }
}

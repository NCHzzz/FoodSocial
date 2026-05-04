import { signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { of } from 'rxjs';
import { vi } from 'vitest';

import { AuthSessionState } from '../../core/auth/auth-session.models';
import { AuthSessionService } from '../../core/auth/auth-session.service';
import { PostApi } from '../../core/posts/infrastructure/post.api';
import { AdminReportsResponse, ContentReportResponse, ResolveContentReportRequest } from '../../core/posts/post.models';
import { ModerationPage } from './moderation.page';

describe('ModerationPage', () => {
  const moderatorId = '11111111-1111-1111-1111-111111111111';
  const report: ContentReportResponse = {
    id: '22222222-2222-2222-2222-222222222222',
    targetType: 'Post',
    targetId: '33333333-3333-3333-3333-333333333333',
    reporterId: '44444444-4444-4444-4444-444444444444',
    reason: 'spam_or_low_quality',
    description: 'Looks suspicious.',
    status: 'Submitted',
    moderatorId: null,
    resolutionNote: null,
    createdAtUtc: '2026-05-04T00:00:00Z',
    resolvedAtUtc: null,
  };
  const reportsResponse: AdminReportsResponse = {
    items: [report],
    limit: 20,
  };
  const moderatorState: AuthSessionState = {
    loadStatus: 'authenticated',
    session: {
      user: {
        subject: 'subject-123',
        preferredUsername: 'moderator-user',
        email: 'mod@example.com',
        roles: ['user', 'moderator'],
      },
      identity: {
        identityId: moderatorId,
        state: 'Active',
        bootstrapPending: false,
        downstreamBootstrapPending: false,
      },
      serverTimeUtc: '2026-05-04T00:00:00Z',
    },
    isLoggingOut: false,
    lastLoadFailed: false,
  };

  let postApiMock: { getAdminReports: ReturnType<typeof vi.fn>; resolveReport: ReturnType<typeof vi.fn> };

  beforeEach(async () => {
    postApiMock = {
      getAdminReports: vi.fn((_limit: number) => of(reportsResponse)),
      resolveReport: vi.fn((_reportId: string, _request: ResolveContentReportRequest) => of({
        ...report,
        status: 'ResolvedNoAction',
        moderatorId,
        resolutionNote: 'Moderator dismissed report from the MVP queue.',
        resolvedAtUtc: '2026-05-04T01:00:00Z',
      })),
    };

    await TestBed.configureTestingModule({
      imports: [ModerationPage],
      providers: [
        provideRouter([]),
        {
          provide: AuthSessionService,
          useValue: {
            authState: signal(moderatorState),
          },
        },
        { provide: PostApi, useValue: postApiMock },
      ],
    }).compileComponents();
  });

  afterEach(() => {
    TestBed.resetTestingModule();
  });

  it('loads the moderator report queue and resolves reports manually', async () => {
    const fixture = TestBed.createComponent(ModerationPage);
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();

    const element = fixture.nativeElement as HTMLElement;
    expect(postApiMock.getAdminReports).toHaveBeenCalledWith(20);
    expect(element.textContent).toContain('Manual queue for reported content');
    expect(element.textContent).toContain('spam_or_low_quality');
    expect(element.textContent).toContain('Looks suspicious.');

    const component = fixture.componentInstance as unknown as {
      resolveReport(reportId: string, action: 'dismiss' | 'hide'): void;
    };
    component.resolveReport(report.id, 'dismiss');
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();

    expect(postApiMock.resolveReport).toHaveBeenCalledWith(report.id, {
      moderatorId,
      action: 'dismiss',
      auditNote: 'Moderator dismissed report from the MVP queue.',
    });
    expect(element.textContent).toContain('Report dismissed.');
    expect(element.textContent).toContain('ResolvedNoAction');
  });
});

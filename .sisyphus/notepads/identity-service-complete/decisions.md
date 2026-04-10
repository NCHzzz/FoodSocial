#ZB|#SR|#PZ|#KJ|Resolved the browser helper by removing fixed-origin rewriting and normalizing all request/callback handling to the actual discovered BFF origin.
#QT|#VX|#KS|#ZV|Kept the browser-session flow on a single discovered BFF origin with no Host spoofing or alternate callback origin.
#WP|#PZ|#JV|#MW|Retained the authenticated session probe, but aligned it to the same browser-origin transport path as the login flow.
#SJ|#ZV|#PX|#TZ|#T8|#BD|Kept bootstrap ownership split: Identity publishes `UserProvisionedIntegrationEvent` and completes lifecycle only after the explicit `UserProfileBootstrapCompletedIntegrationEvent` arrives back from UserProfile.
#QN|#PT|#XZ|#TZ|#T8|#ID|Kept the profile shell minimal and idempotent: a single `user_profile_shells` row is keyed by identity/subject uniqueness, and duplicate deliveries resolve to the existing shell instead of creating new rows.
#KY|#BV|#SY|
#XZ|#BV|#JZ|## 2026-04-10 Task 9 decisions
#TS|#HX|#VN|- Kept production-path verification on the existing AppHost test surface and avoided creating a parallel harness.
#QQ|#JQ|#HV|- Chose deterministic degraded-case simulation at persistence boundaries (outbox delivery-state mutation and temporary UserProfile insert-blocking constraint) to verify failure semantics without destabilizing shared runtime services.
#HB|#NR|#TZ|#T9|#FX|- Kept authorization ownership with Identity: `GET /api/auth/session` now emits only local app roles from the ensured identity response, while `/identity/current-user` loads the stored identity row for truthful `identityId` and lifecycle state.
#SQ|#HZ|#TZ|#T9|#VL|- Replaced controller-thrown admin input validation with explicit 400 ProblemDetails responses for null body, blank subject/role, and unsupported role values.
#VQ|- Final-wave reviewer decision: evidence the trusted ensure path with tests instead of broadening production code; prove that only the BFF edge can use the internal ensure transport and that public/user callers are rejected.
#QN|#FW|- Final-wave hardening decision: remove caller-controlled `subject` from `TrustedEnsureIdentityRequest` and keep internal ensure subject authority claim-driven.
#RT|#FW|- BFF internal ensure now forwards the authenticated access token together with trust headers; headers authorize transport while bearer claims authorize subject selection.

#RJ|#YS|#ZH|#MH|#QJ|The previous helper mixed fixed-origin callback detection with discovered-origin transport, which drifted the OIDC callback path during the login POST.
#XB|#XM|#YQ|#SH|#KP|Removed the post-login `/api/auth/session` probe from the browser helper so the flow ends after the authenticated cookie/session is established.
#JW|#HR|#TN|#ZY|#HZ|The real runtime blocker was a schema mismatch: the live Identity database lacked `identity_users.State`, so `/api/auth/session` failed when the BFF called into Identity during the authenticated browser flow.
#QH|#WK|#TZ|#T8|#SC|Task 8 exposed a second live-schema gap: UserProfile had no EF migrations, so the shell table never existed in AppHost-backed verification until the initial migration was added.
#XH|#VH|#TZ|#T8|#TV|The first failing AppHost assertion after the runtime fix was in the test verifier itself: PostgreSQL column lookup used `identity_id`, but the EF-generated column name was `IdentityId`.
#BV|#SY|
#NZ|#VS|## 2026-04-10 Task 9 issues
#KV|#BS|- Direct service/process stop simulation introduced cross-test interference inside the shared AppHost collection and produced unrelated connectivity failures.
#VY|#XN|- RBAC "allow" verification through a long-lived authenticated Identity client can remain forbidden if that bearer token does not represent the same subject whose DB role assignment was mutated during the test.
#XT|#TZ|#T9|#RF|- Final-review rejection surfaced three concrete defects: BFF role leakage from token claims, `/identity/current-user` returning placeholder local identity state, and admin role-assignment using exception-path validation for bad client input.
- Final-wave cleanup note: `.omx/` and `_tmp_aspire.dev/` were safe to remove from repo state; `FoodSocialaspire-temp/` remained locked by another local process during cleanup and should be removed once that handle is released.

## Archived Capability Summary

### Guest users can discover and start sign-in from shared web navigation
- Guest navigation shows a login action.
- Public browsing remains available.

### Authenticated users can identify their signed-in state from shared web navigation
- Authenticated navigation hides the guest login action.
- Authenticated-state auth controls become visible instead.

### Authenticated users can sign out from the web frontend
- Logout uses the existing POST-oriented BFF flow.
- Successful logout returns the UI to guest-safe presentation.

### Session-aware auth UI fails safely for public browsing
- Unresolved or failed session checks fall back to guest-safe state.

## Historical Note

This archived OpenSpec spec is retained as change history, not as the repo’s canonical product truth.

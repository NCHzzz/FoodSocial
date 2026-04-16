# FoodSocial Frontend

This directory is the production Angular 21 frontend workspace for FoodSocial.

The workspace is rooted directly at `Frontend/` and follows the repository’s Angular 21 production direction:

- standalone bootstrap and standalone components only
- `src/app/core`, `src/app/shell`, and lazy `src/app/features/*` route areas
- signals for local and view-oriented state
- strict TypeScript + strict Angular template diagnostics
- TailwindCSS v4 as the primary styling layer
- BFF-owned browser auth integration through `/api/auth/*`

The repository's canonical frontend working rules live in:

- `AGENTS.md`
- `Frontend/AGENTS.md`
- `../docs/ai/workflows.md`
- `../docs/ai/stack-rules/angular-tailwind.md`
- `../docs/ai/stack-rules/frontend-ui-unification.md` for visual/UI work

## Development server

To start a local development server, run:

```bash
npm start
```

Once the server is running, open your browser and navigate to `http://localhost:4200/`.

The Angular dev server proxies auth-related browser routes to the BFF using `proxy.conf.json`. For login/logout verification to work locally, the Aspire/AppHost stack and Keycloak resource must also be running healthy.

## Workspace structure

```text
Frontend/
├── angular.json
├── package.json
├── proxy.conf.json
├── src/
│   ├── main.ts
│   ├── styles.css
│   └── app/
│       ├── app.config.ts
│       ├── app.routes.ts
│       ├── core/
│       ├── shell/
│       └── features/
```

## Architecture notes

- `core/` holds app-wide infrastructure such as auth/session services, browser abstractions, and guards.
- `shell/` owns the app frame, navigation, and auth-aware layout.
- `features/` holds lazy route areas and page-level composition.
- Public browsing routes stay distinct from authenticated-only route areas.

## Code scaffolding

Angular CLI includes powerful code scaffolding tools. To generate a new component, run:

```bash
ng generate component component-name
```

Prefer placing new code into the existing `core/`, `shell/`, or feature route area instead of generating flat files directly into `src/app/`.

For a complete list of available schematics (such as `components`, `directives`, or `pipes`), run:

```bash
ng generate --help
```

## Building

To build the project run:

```bash
ng build
```

This will compile your project and store the build artifacts in the `dist/` directory. By default, the production build optimizes your application for performance and speed.

## Running unit tests

To execute unit tests with the [Vitest](https://vitest.dev/) test runner, use the following command:

```bash
ng test
```

The workspace uses the Angular/Vitest unit-test integration configured at the root `Frontend/` level.

## Running end-to-end tests

Manual auth verification should be performed with the local Aspire/AppHost platform running, so the Angular shell can round-trip through the BFF and Keycloak.

Suggested local sequence:

```bash
npm start
# then start the local backend/AppHost stack from the repo root or Backend area
```

## Additional Resources

- `../docs/ai/stack-rules/angular-tailwind.md` — repo frontend engineering rules
- `../docs/ai/stack-rules/frontend-ui-unification.md` — canonical FoodSocial UI rules for visual/frontend implementation
- [Angular CLI Overview and Command Reference](https://angular.dev/tools/cli)

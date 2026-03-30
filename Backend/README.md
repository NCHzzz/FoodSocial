# Backend

Starter scaffold for a .NET 10 microservices backend.

## Layout

- `building-blocks/` shared cross-cutting code and templates for future reuse.
- `gateways/` edge-facing gateway or BFF hosts.
- `hosts/` local orchestration or shared host entry points.
- `services/` business services, each following the same clean-architecture slice.
- `deploy/` deployment placeholders and environment-specific infra notes.
- `docs/` backend-specific design notes and conventions.

## Service pattern

Each service follows the same repeatable shape:

- `src/<Service>.Api`
- `src/<Service>.Application`
- `src/<Service>.Domain`
- `src/<Service>.Infrastructure`
- `tests/<Service>.UnitTests`
- `tests/<Service>.IntegrationTests`

This scaffold is intentionally non-runnable and exists only to preserve structure for future implementation.

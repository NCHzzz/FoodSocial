# CLAUDE.md

## Project Context

Read `AGENTS.md` first. This file is a thin adapter only. Do not treat it as a separate source of truth.

## Documentation Hierarchy

1. `AGENTS.md` - Primary agent configuration
2. `docs/ai/repo-map.md` - Repository structure
3. `docs/project-discovery/*.md` - Project documentation
4. `docs/ai/workflows.md` - Development workflows

## Quick Start

### Development Setup
```bash
# Install dependencies
npm install

# Start development server
npm run dev

# Build for production
npm run build

# Start production server
npm start
```

### Database Setup
```bash
# Run migrations
npm run db:migrate

# Seed initial data
npm run db:seed

# Reset database
npm run db:reset
```

### Documentation
- [Project Overview](./docs/project-discovery/00_project_overview.md)
- [Business Context](./docs/project-discovery/01_business_context.md)
- [Personas & Roles](./docs/project-discovery/02_personas_and_roles.md)
- [MVP Scope](./docs/project-discovery/03_scope_mvp.md)
- [Functional Requirements](./docs/project-discovery/04_functional_requirements.md)
- [User Flows](./docs/project-discovery/06_user_flows.md)
- [Data Model](./docs/project-discovery/07_data_model.md)
- [API Specification](./docs/project-discovery/08_api_spec.md)
- [Architecture](./docs/project-discovery/10_architecture.md)

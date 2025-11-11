# Holmes Delivery Plan

This roadmap assumes the ASP.NET Core host (`src/Holmes.App.Server`) fronts APIs, SSE endpoints, and background workers,
with a developer MCP sidecar (`src/Holmes.Mcp.Server`) exposing tool endpoints. Feature slices live under `src/Modules/`
and compose into the host through each module's Application + Infrastructure packages. Each phase bundles deployable
value, expands observability, and keeps the event-sourced backbone intact.

---

## 0. Baseline Constraints

- **Runtime:** .NET 8 (Minimal APIs, background services, file-scoped namespaces).
- **Database:** MySQL 8 with EF Core migrations (Pomelo provider).
- **Identifiers:** ULID per aggregate; global `events.position` as `BIGINT`.
- **Event Store:** Append-only `events` table + optional snapshots; projections checkpointed.
- **Streaming:** Server-Sent Events `/changes` with tenant + stream filters.
- **Security/Audit/Compliance:** JWT auth, tenant isolation, AES-GCM for PII, secrets via user-secrets/env vars. Every
  aggregate mutation writes an immutable `EventRecord` (
  `src/Modules/Core/Holmes.Core.Infrastructure.Sql/Entities/EventRecord.cs`) so regulators can replay history, and no
  customer data ever crosses tenant boundaries—FCRA/EEOC/ICRAA readiness is a 100% requirement.
- **Tooling:** `dotnet format`, GitHub Actions (later), Docker Compose for dev MySQL.
- **Module layering:** `Holmes.Core.*` supplies shared primitives; each feature module is split into `*.Domain`,
  `*.Application`, `*.Infrastructure` with `*.Application` → `*.Domain` dependencies only and `*.Infrastructure` wiring
  into hosts without referencing `*.Application`.

---

## 1. Solution Skeleton

```
/src
  Holmes.App.Server/                 # ASP.NET Core host (APIs, SSE, background services)
  Holmes.App.Server.Tests/           # Host-level integration/acceptance tests
  Holmes.Client/                     # React workspace (components/, pages/, models/, lib/)
  Holmes.Mcp.Server/                 # Dev MCP sidecar exposing tool endpoints
  Modules/
    Core/
      Holmes.Core.Domain/            # Value objects, integration events, policies
      Holmes.Core.Application/       # Behaviors, pipeline, cross-cutting services
      Holmes.Core.Infrastructure.Sql/ # EF Core base context, migrations
      Holmes.Core.Infrastructure.OpenAi/
      Holmes.Core.Tests/
    Subjects/
      Holmes.Subjects.Domain/
      Holmes.Subjects.Application/
      Holmes.Subjects.Infrastructure.Sql/
      Holmes.Subjects.Tests/
    Users/
      Holmes.Users.Domain/
      Holmes.Users.Application/
      Holmes.Users.Infrastructure.Sql/
    Customers/
      Holmes.Customers.Domain/
      Holmes.Customers.Application/
      Holmes.Customers.Infrastructure.Sql/
    Intake/
      Holmes.Intake.Domain/
      Holmes.Intake.Application/
      Holmes.Intake.Infrastructure.Sql/
      Holmes.Intake.Tests/
    Workflow/
      Holmes.Workflow.Domain/
      Holmes.Workflow.Application/
      Holmes.Workflow.Infrastructure.Sql/
    SlaClocks/
      Holmes.SlaClocks.Domain/
      Holmes.SlaClocks.Application/
      Holmes.SlaClocks.Infrastructure.Sql/
    Compliance/
      Holmes.Compliance.Domain/
      Holmes.Compliance.Application/
      Holmes.Compliance.Infrastructure.Sql/
    Notifications/
      Holmes.Notifications.Domain/
      Holmes.Notifications.Application/
      Holmes.Notifications.Infrastructure.Sql/
    AdverseAction/
      Holmes.AdverseAction.Domain/
      Holmes.AdverseAction.Application/
      Holmes.AdverseAction.Infrastructure.Sql/
    Adjudication/
      Holmes.Adjudication.Domain/
      Holmes.Adjudication.Application/
      Holmes.Adjudication.Infrastructure.Sql/
    ChargeTaxonomy/
      Holmes.ChargeTaxonomy.Domain/
      Holmes.ChargeTaxonomy.Application/
      Holmes.ChargeTaxonomy.Infrastructure.Sql/
  Projections/
    Holmes.Projections.Runner/       # Projection runners + read-model DbContexts
    Holmes.Projections.Tests/
/tests
  Holmes.Tests.Unit/
  Holmes.Tests.Integration/
```

Each module compiles to a class library exposing domain + application services. Holmes.App.Server references modules and
Infrastructure to compose the runtime.

---

## 2. Phase Roadmap

### Phase 0 — Bootstrap & Infrastructure

**Modules touched:** Holmes.Core.*, Holmes.App.Server scaffold, Holmes.Mcp.Server  
**Outcomes**

- `Holmes.App.Server` minimal host with health endpoint, Serilog, config, env-based settings.
- `Holmes.Mcp.Server` stub with discovery + single tool endpoint for local orchestration.
- Holmes.Core primitives (`UlidId`, `Result<T>`, `ValueObject`, pipeline behaviors, crypto stubs).
- Event store EF Core model (`events`, `snapshots`, `projection_checkpoints`) with optimistic concurrency + idempotency
  key.
- Projection runner base class + background registration.
- Docker Compose for MySQL; initial migration + seeding script.
- CI lint/build workflow.

### Phase 1 — Identity & Tenancy Foundations

**Modules delivered:** Holmes.Core (domain/app), Subjects, Users, Customers  
**Outcomes**

- Aggregates + handlers for `Subject`, `User`, `Customer`.
- Subject Registry scaffolding (aggregate, commands, EF infrastructure) ready for intake + policy linkage.
- Users module cadence:
    - Define `User`, `RoleAssignment`, `ExternalIdentity` aggregates/events (no credential storage).
    - Scaffold projections (`user_directory`, `user_role_memberships`) that power authorization policies.
    - Expose commands for register/activate external users, grant/revoke roles, suspend/reactivate.
    - Integrate HTTP middleware to map OIDC tokens → Holmes roles via read models.
- Tenant-aware policy snapshot + customer assignment to orders.
- Identity endpoints (invite/activate user) with tenancy + audit trails.
- Read models: `subject_summary`, `user_directory`, `customer_registry`.
- Integration tests for user activation, subject merge, customer assignment flows.
- Observability: structured logging, request tracing, basic metrics.

**Follow-ups (rolled into Phase 1.5)**

- Align `Holmes.Core` module conventions with the finalized Users & Customers modules.
- Capture a concise UnitOfWork/domain-event dispatch overview so future modules follow the shared pattern.

### Phase 1.5 — Platform Cohesion & Event Plumbing

**Modules delivered:** Holmes.Core, Holmes.App.Server, Users, Customers, Subjects, Holmes.Identity.Server,
Holmes.Client  
**Outcomes**

- Shared infrastructure: integration specs in `Holmes.Core.Tests` guard the `UnitOfWork<TContext>` + domain-event
  dispatch pipeline (multi-aggregate commit, rollback safety) and the pattern is documented in ARCHITECTURE so every
  module copies the same transactional template.
- Identity + tenancy plumbing:
    - `Holmes.Identity.Server` runs as the dev IdP; `DevelopmentDataSeeder` mirrors the IdP admin, grants the Admin
      role,
      and creates a seeded customer profile/contact so invite → activate → assign role works immediately.
    - Authorization helpers back the new `GET /api/users`, `POST /api/users/invitations`, and role mutation endpoints;
      the React
      role union (`Admin`, `CustomerAdmin`, `Compliance`, `Operations`, `Auditor`) matches the backend enums.
- Customer contract alignment:
    - Introduced `customer_profiles` + `customer_contacts` projections/migrations and exposed paginated
      `CustomerListItemResponse` payloads from `GET/POST /api/customers`, so the React table/form consumes the real
      DTOs.
    - `docs/DEV_SETUP.md` now walks through `pwsh ./ef-reset.ps1`, which *always* drops databases, deletes migration
      folders, scaffolds initial migrations (plus `AddCustomerProfiles`), and reapplies them for Core/Users/Customers/
      Subjects.
- Subject registry readiness: `GET /api/subjects?page=&pageSize=` and `POST /api/subjects/merge` feed the UI proof
  screen with
  actual data and wire through `MergeSubjectCommand`.
- SPA proof surface: `Holmes.Client` runs under Vite + SPA proxy, hits the aforementioned APIs via TanStack Query,
  redirects 401 responses to `/auth/options`, and proves Phase 1 flows (invite, grant/revoke role, create customer,
  merge subject) without Postman.
- Observability + docs: structured logging + correlation IDs are standard, and ARCHITECTURE + DEV_SETUP call out IdP
  setup, seeding, reset instructions, and the SPA proxy steps so new devs can follow a deterministic recipe.

**Acceptance**

- `pwsh ./ef-reset.ps1` rebuilds every schema (Core, Users, Customers, Subjects) and reapplies the Initial +
  `AddCustomerProfiles` migrations without manual cleanup or selective skipping.
- Running `dotnet run` (Holmes.App.Server) alongside `npm run dev` (Holmes.Client) allows a tenant admin to complete
  invite → activate → grant/revoke role → create customer → merge subject entirely through the UI, with a single
  domain-event batch per transaction and green `dotnet test` / `npm run build` pipelines.

### Phase 1.8 — Auth Flow Cleanup & Hardening

**Modules delivered:** Holmes.App.Server, Holmes.Client, Holmes.Identity.Server  
**Outcomes**

- Holmes.App.Server owns the entire auth challenge experience: unauthenticated HTML requests short-circuit to
  `/auth/options`, which renders the provider list server-side (sanitized `returnUrl`, no SPA boot required).
- `Holmes.Client`'s `AuthBoundary` simply retries `/users/me`; if it ever receives 401/403/404, it performs a full-page
  navigation back through `/auth/options?returnUrl=…`, keeping provider choice and cookie issuance on the server.
- OpenID Connect logins now rely on middleware + `RegisterExternalUserCommand` to ensure the Holmes user already exists.
  Uninvited attempts publish `UninvitedExternalLoginAttempted`, are logged, and the session is cleared + redirected to
  `/auth/access-denied`.
- Invited users flow straight from `Invited` to `Active` on successful login; no manual approval
  endpoints or pending lists are exposed.
- Added integration coverage that asserts `/users` (HTML) produces a 302 to `/auth/options`, so regressions in the
  middleware are caught alongside the existing API tests.
- `Holmes.Identity.Server` stays a local-only Duende stub; the docs now flag it as dev-only plumbing so it never makes
  it
  into CI/CD environments.

### Phase 1.9 — Foundation Hardening & UX Architecture

**Modules delivered:** Holmes.App.Server, Holmes.Client, Holmes.Core, Users, Customers, Subjects  
**Objectives**

- **Deferred MCP scope:** explicitly move the developer MCP sidecar into the backlog so Phase 2 can start without
  pretending the feature exists. Track it (with requirements) in the backlog board for a later phase.
- **Observability debt paydown:** add projection/unit-of-work metrics, tracing, and dashboards/alerts so event replay
  health is visible before adding new bounded contexts.
- **Automated coverage:** stand up `Holmes.Subjects.Tests`, `Holmes.Customers.Tests`, and expand `Holmes.Users.Tests` to
  cover aggregate invariants plus EF integration slices. CI must run them alongside `dotnet test`.
- **Read-model verification:** ensure subjects/customers directories, admin assignments, and SLA-ready projections match
  the documented behaviors; document verification steps in DEV_SETUP.
- **Operational runbooks:** author deterministic guides for `ef-reset`, Dockerized MySQL resets, and projection replays.
  See `docs/RUNBOOKS.md` for the shared playbook covering resets, projection verification, and observability hookup.
- **Holmes.Client architecture pass:** working within the current stack (`react`, `react-router-dom`, `@mui/*`,
  `@tanstack/react-query`), define design tokens, layout primitives, route conventions, and query hooks so future flows
  drop in without re-plumbing.
- **UX refresh:** partner with Rebecca Wirfs-Brock’s recommended UX collaborators to replace placeholder CRUD layouts
  with domain-first surfaces (subject timeline, SLA badges, audit panels, role badges). Capture their component library
  guidelines in docs.
- **Readiness review:** only when the above items are closed can Phase 2 begin; the review is documented with links to
  metrics dashboards, test reports, and updated UI architecture notes.

### Phase 2 — Intake & Workflow Launch

**Modules delivered:** Intake, Workflow, Subjects enhancements  
**Outcomes**

- Aggregates + handlers for `IntakeSession`, `Order` workflow states.
- REST endpoints for invite → submit flow; subject linkage + policy snapshot enforcement.
- Read models: `order_summary`, `order_timeline_events`, `intake_sessions`.
- SSE `/changes` endpoint delivering ordered event frames with filters + heartbeats.
- Integration tests for intake flow, SSE resume, optimistic concurrency.
- Initial PWA scaffolding within `Holmes.Client` for intake experience.

### Phase 3 — SLA, Compliance & Notifications

**Modules delivered:** SlaClocks, Compliance, Notifications (baseline)  
**Outcomes**

- Business calendar service + EF models for calendars/holidays.
- Aggregates: `SlaClock`, `CompliancePolicy`, `PermissiblePurposeGrant`, `DisclosurePack`.
- Guards wired into order workflow (PP grant, disclosure acceptance, customer policy overlays).

### Backlog / Deferred Items

- **Developer MCP Sidecar:** Originally scoped for Phase 0; explicitly deferred until after Phase 2 so we can ship core
  intake/workflow value first. Requirements (tool discovery, auth, local orchestration) stay in the backlog column and
  will be re-estimated once Phase 2 lands.
- Watchdog background worker flipping clock states; read model `sla_clocks`.
- Notification rules v1 (email/SMS/webhook stubs) firing on domain events, stored in `notifications_history`.
- Dashboard-ready metrics: SLA status counts, notification success/failure.

### Phase 4 — Adverse Action & Evidence Packs

**Modules delivered:** AdverseAction, Artifacts (within Infrastructure)  
**Outcomes**

- State machine for pre/final adverse action, pause/resume, disputes linkage.
- WORM artifact storage abstraction (local dev filesystem) with hash validation.
- Evidence pack bundler (zip of PDFs + JSON manifest).
- Read model `adverse_action_clocks` + API endpoints for regulators/ops.
- Tests covering clock pauses, artifact integrity, policy-driven wait periods.

### Phase 5 — Adjudication Engine

**Modules delivered:** Adjudication, ChargeTaxonomy, Notifications enhancements  
**Outcomes**

- RuleSet authoring + publish workflow; persisted snapshots per order.
- Deterministic assessment engine generating reason codes, recommended outcome.
- Queue/read model for reviewer workload (`adjudication_queue`, `assessment_summary`).
- Human override flow with justification + attachment references.
- Notifications enriched with adjudication triggers; SSE events for assessment changes.
- Simulator API for what-if runs (bounded scope).

### Phase 6 — Hardening & Pilot Readiness

**Modules matured:** All  
**Outcomes**

- Tenant branding/localization hooks; policy snapshot UI contract.
- Observability dashboards for SLA, adverse, adjudication throughput.
- Property & chaos tests (duplicate events, out-of-order, SSE reconnect storms).
- Performance tuning (projection replay, SSE throughput, MySQL indexes).
- Deployment automation (container image, migrations) and runbooks.

---

## 3. Cross-Phase Practices

- **Testing:** Unit + integration coverage per module, projection replay tests, SSE resilience harness.
- **Security:** Secrets via `dotnet user-secrets` for dev, env variables for higher tiers; AES-GCM encryption utilities
  ready by Phase 2.
- **Documentation:** Update DESIGN.md per phase completion; keep API contracts in `docs/` (future).
- **Readiness Gates:** Each phase exits with automated tests green, migrations applied, SSE verified with Last-Event-ID
  resume, and key dashboards updated.

This structure should make it obvious which modules deliver in each phase and keeps us focused on incremental,
deployable milestones. Adjust module sequencing as needed, but keep event-store integrity and SSE reliability as
non-negotiable acceptance criteria throughout.

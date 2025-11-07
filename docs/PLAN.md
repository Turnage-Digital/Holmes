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
- **Security:** JWT auth, tenant isolation, AES-GCM for PII, secrets via user-secrets/env vars.
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
    SubjectRegistry/
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

**Modules delivered:** Holmes.Core (domain/app), SubjectRegistry, Users, Customers  
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

**Modules delivered:** Holmes.Core, Holmes.App.Server, Users, Customers, SubjectRegistry, Holmes.Client  
**Outcomes**

- Harden the shared `UnitOfWork<TContext>` and domain-event dispatch path with integration tests (multi-aggregate
  transaction, rollback safety) and cross-module documentation.
- Promote the identity/tenant read models into `Holmes.App.Server` (authorization helpers, policies, seeding) so Intake
  work can assume a consistent host surface.
- Normalize module template conventions (namespace layout, dependency graph, base behaviors) so every new module copies
  the same scaffolding.
- Expand baseline observability (structured logging, tracing correlation IDs, seed scripts, DB reset tooling) to make
  future module debugging repeatable.
- Land the React SPA scaffold (`Holmes.Client`) with Vite + SPA proxy wiring so the server can proxy to `npm run dev`
  during local development and serve the static build in CI/CD.
- Build “Phase 1 proof” UI flows: tenant switcher stub + admin views to list users/customers/subjects, invite a user,
  grant/revoke a role, create/link a customer, and view subject merges by calling the live APIs.
- Ship generated/hand-authored TypeScript clients (OpenAPI or minimal fetch wrappers) plus test fixtures so front-end
  calls stay in sync with backend DTOs.
- Add smoke tests (Playwright/component-level) that run a happy-path invite → activate → assign-role → create-customer
  scenario end-to-end via the SPA proxy to prove Phase 1 is usable through the UI.

**Acceptance**

- Developers can run the host, execute user/customer flows end-to-end with domain events dispatching exactly once per
  commit, and new modules can be scaffolded without manual fixing.
- Running `dotnet run` on Holmes.App.Server launches the SPA proxy to `Holmes.Client`, and a tenant admin can complete
  invite → activate → assign role → create customer strictly through the React UI with telemetry showing the emitted
  domain events.

### Phase 2 — Intake & Workflow Launch

**Modules delivered:** Intake, Workflow, SubjectRegistry enhancements  
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

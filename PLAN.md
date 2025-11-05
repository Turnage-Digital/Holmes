# Holmes Delivery Plan

This roadmap assumes a single deployment target (`src/Holmes.Server`) hosting APIs, SSE endpoints, and background workers. We build bounded-context modules as separate projects that plug into the server through an Infrastructure layer using EF Core + MySQL. Each phase bundles deployable value, expands observability, and keeps the event-sourced backbone intact.

---

## 0. Baseline Constraints

- **Runtime:** .NET 8 (Minimal APIs, background services, file-scoped namespaces).
- **Database:** MySQL 8 with EF Core migrations (Pomelo provider).
- **Identifiers:** ULID per aggregate; global `events.position` as `BIGINT`.
- **Event Store:** Append-only `events` table + optional snapshots; projections checkpointed.
- **Streaming:** Server-Sent Events `/changes` with tenant + stream filters.
- **Security:** JWT auth, tenant isolation, AES-GCM for PII, secrets via user-secrets/env vars.
- **Tooling:** `dotnet format`, GitHub Actions (later), Docker Compose for dev MySQL.

---

## 1. Solution Skeleton

```
/src
  Holmes.Server/              # Host project (APIs, SSE, background services)
  SharedKernel/               # Value objects, ULID, Result<T>, cryptography helpers
  EventStore/                 # Event store abstraction, EF Core contexts, schema
  Infrastructure/             # DbContexts, module registration, migrations
  Modules/
    SubjectRegistry/
    Intake/
    Workflow/
    SlaClocks/
    Compliance/
    AdverseAction/
    Notifications/
    Adjudication/
  Projections/                # Projection runners + read-model DbContexts
/tests
  Unit/
  Integration/
  Projections/
```

Each module compiles to a class library exposing domain + application services. Holmes.Server references modules and Infrastructure to compose the runtime.

---

## 2. Phase Roadmap

### Phase 0 — Bootstrap & Infrastructure
**Modules touched:** SharedKernel, EventStore, Infrastructure, Holmes.Server scaffold  
**Outcomes**
- `Holmes.Server` minimal host with health endpoint, Serilog, config, env-based settings.
- SharedKernel primitives (`UlidId`, `Result<T>`, `ValueObject`, `ClockService`, crypto stubs).
- Event store EF Core model (`events`, `snapshots`, `projection_checkpoints`) with optimistic concurrency + idempotency key.
- Projection runner base class + background registration.
- Docker Compose for MySQL; initial migration + seeding script.
- CI lint/build workflow.

### Phase 1 — Core Domain Foundations
**Modules delivered:** SubjectRegistry, Intake, Workflow, Projections  
**Outcomes**
- Aggregates + handlers for `Subject`, `IntakeSession`, `Order`.
- REST endpoints for invite → submit flow; basic auth stub.
- Read models: `subject_summary`, `order_summary`, `order_timeline_events`.
- SSE `/changes` endpoint delivering ordered event frames with filters + heartbeats.
- Integration tests for intake flow, SSE resume, optimistic concurrency.
- Observability: structured logging, request tracing, basic metrics.

### Phase 2 — SLA & Compliance Launch
**Modules delivered:** SlaClocks, Compliance, Notifications (baseline)  
**Outcomes**
- Business calendar service + EF models for calendars/holidays.
- Aggregates: `SlaClock`, `CompliancePolicy`, `PermissiblePurposeGrant`, `DisclosurePack`.
- Guards wired into order workflow (PP grant, disclosure acceptance gates).
- Watchdog background worker flipping clock states; read model `sla_clocks`.
- Notification rules v1 (email/SMS/webhook stubs) firing on domain events, stored in `notifications_history`.
- Dashboard-ready metrics: SLA status counts, notification success/failure.

### Phase 3 — Adverse Action & Evidence Packs
**Modules delivered:** AdverseAction, Artifacts (within Infrastructure)  
**Outcomes**
- State machine for pre/final adverse action, pause/resume, disputes linkage.
- WORM artifact storage abstraction (local dev filesystem) with hash validation.
- Evidence pack bundler (zip of PDFs + JSON manifest).
- Read model `adverse_action_clocks` + API endpoints for regulators/ops.
- Tests covering clock pauses, artifact integrity, policy-driven wait periods.

### Phase 4 — Adjudication Engine
**Modules delivered:** Adjudication, ChargeTaxonomy, Notifications enhancements  
**Outcomes**
- RuleSet authoring + publish workflow; persisted snapshots per order.
- Deterministic assessment engine generating reason codes, recommended outcome.
- Queue/read model for reviewer workload (`adjudication_queue`, `assessment_summary`).
- Human override flow with justification + attachment references.
- Notifications enriched with adjudication triggers; SSE events for assessment changes.
- Simulator API for what-if runs (bounded scope).

### Phase 5 — Hardening & Pilot Readiness
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
- **Security:** Secrets via `dotnet user-secrets` for dev, env variables for higher tiers; AES-GCM encryption utilities ready by Phase 2.
- **Documentation:** Update DESIGN.md per phase completion; keep API contracts in `docs/` (future).
- **Readiness Gates:** Each phase exits with automated tests green, migrations applied, SSE verified with Last-Event-ID resume, and key dashboards updated.

This structure should make it obvious which modules deliver in each phase and keeps us focused on incremental, deployable milestones. Adjust module sequencing as needed, but keep event-store integrity and SSE reliability as non-negotiable acceptance criteria throughout.

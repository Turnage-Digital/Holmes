# Holmes Delivery Plan – Updated for Phase 3–6 Alignment

This updated PLAN.md clarifies the work for Phases 3–6, aligns roadmap items with the Compliance Suite and Identity
Broker architecture, and modernizes the language around notifications, evidence packs, adjudication, and white-label
readiness.

---
## Context Instructions

Do not include "Generated with Claude Code" or Co-Authored-By lines in commits or PRs.

### Module Architecture

This is a .NET 9 modular monolith following Clean Architecture and Domain-Driven Design principles. Each bounded
context is a self-contained module under `src/Modules/{ModuleName}/` with these projects:

1. `Holmes.{Module}.Domain` - The core domain layer:
   - Aggregate roots (e.g., `Customer.cs`, `SlaClock.cs`)
   - Value objects under `ValueObjects/`
   - Domain events under `Events/` (e.g., `CustomerRegistered.cs`)
   - Repository interfaces (`I{Entity}Repository.cs`)
   - Unit of Work interface (`I{Module}UnitOfWork.cs`)
   - Enums for entity states

2. `Holmes.{Module}.Application` - Application/use case layer:
   - Commands under `Commands/` (CQRS command handlers)
   - Queries under `Queries/` (CQRS query handlers)
   - Event handlers under `EventHandlers/` for domain event projections and side effects

3. `Holmes.{Module}.Application.Abstractions` - Contracts for the application layer:
   - DTOs under `Dtos/`
   - Query interfaces under `Queries/` (e.g., `I{Entity}Queries.cs`)
   - Projection writer interfaces under `Projections/`
   - Service interfaces under `Services/`
   - Notification/broadcaster interfaces under `Notifications/`

4. `Holmes.{Module}.Infrastructure.Sql` - EF Core persistence:
   - `{Module}DbContext.cs`
   - `{Module}UnitOfWork.cs`
   - Database entities under `Entities/` (e.g., `CustomerDb.cs`)
   - Repositories under `Repositories/` implementing domain interfaces
   - Query implementations under `Queries/` (implementing interfaces from Application.Abstractions)
   - Projections under `Projections/`
   - Mappers under `Mappers/`
   - Specifications under `Specifications/`
   - EF Migrations under `Migrations/`
   - `DependencyInjection.cs` for service registration

5. `Holmes.{Module}.Tests` - Unit tests for the module

### Dependency graph:

```
Application ──────► Application.Abstractions ◄────── Infrastructure.Sql
     │                        │                              │
     └────────► Domain ◄──────┴──────────────────────────────┘
```

- Domain has no dependencies (pure domain logic)
- Application.Abstractions depends on Domain (for value objects in DTOs)
- Application depends on Domain and Application.Abstractions
- Infrastructure.Sql depends on Domain and Application.Abstractions (NOT Application)

### Cross-module references:

When ModuleA needs types from ModuleB:
- Allowed: `ModuleA.Application` → `ModuleB.Application.Abstractions`
- Allowed: `ModuleA.Infrastructure.Sql` → `ModuleB.Application.Abstractions`
- Forbidden: Direct references to another module's Domain, Application, or Infrastructure.Sql

### Cross-module integration handlers:

Cross-module integration handlers live in the consuming module's Application, and may depend on producer
Abstractions (integration events only).

### App projects

- `Holmes.App.Infrastructure.Security` - Host security/identity wiring and policies.

### Key patterns:
- CQRS (Command Query Responsibility Segregation)
- Domain Events for cross-module communication
- Event-driven projections for read models
- Unit of Work pattern per module
- Separate database entities (`*Db.cs`) from domain entities
- Abstractions layer to prevent circular dependencies
- No cross-module transactions; use outbox + integration events between modules

---

# Phase 3 — SLA, Compliance & Notifications

**Modules delivered:** SlaClocks, Compliance, Notifications (baseline)

**Outcomes**

- Business calendar service + EF models for calendars/holidays.
- Aggregates delivered:
    - `SlaClock`
    - `CompliancePolicy`
    - `PermissiblePurposeGrant`
    - `DisclosurePack`
- Order workflow protections:
    - Permissible Purpose guardrails
    - Disclosure acceptance evidence
    - Customer policy overlays applied during Intake → Workflow lifecycle
- Initial Compliance bounded context foundations (Phase 3 of Compliance Suite)
- Baseline Notifications:
    - Tenant-configured provider abstractions (email/SMS/webhook)
    - Holmes emits `NotificationCreated` events; providers deliver
- Identity Broker readiness:
    - Holmes.Identity can federate with tenant IdPs via OIDC
    - Tenant-scoped identity mapping and role assignment

---

# Phase 4 — Adverse Action & Evidence Packs

**Modules delivered:** AdverseAction, Artifacts (Infrastructure), Compliance extensions

**Outcomes**

- **Adverse Action State Machine:**
    - Pre-adverse → waiting period → final notice workflow
    - Pause/resume on disputes
    - Policy-driven wait period calculations
    - Regulatory-compliant clock enforcement
- **Evidence Packs:**
    - Deterministic bundler (ZIP of PDFs + JSON manifest)
    - Containing consent, policy snapshots, notices, artifacts, dispute thread, timeline events
- **WORM Artifact Store:**
    - Write-once model with hash validation
    - Backed by encrypted MySQL BLOBs (swappable to Azure Blob Storage)
- **Dispute Case Integration:**
    - Dispute lifecycle primitives ready for Phase 5 integration
- **Regulator/Operations APIs:**
    - Read models: `adverse_action_clocks`, `adverse_action_cases`
    - Clock pause/resume
    - Evidence pack retrieval endpoints
- **Tests:**
    - Clock boundary conditions
    - Policy overlays
    - Artifact integrity
    - State transitions

---

# Phase 5 — Adjudication Engine

**Modules delivered:** Adjudication, ChargeTaxonomy, Notifications enhancements

**Outcomes**

- **RuleSet Authoring + Publish Workflow:**
    - Tenant-scoped rule definitions and versioning
    - Persisted snapshot per order for auditability
- **Deterministic Assessment Engine:**
    - Generates reason codes and recommended outcomes
    - Deterministic re-evaluation from snapshots
- **Reviewer Queue:**
    - `adjudication_queue` projection
    - Workload routing and escalation
- **Assessment Summary Read Model:**
    - Order-level classification, reviewer notes, override history
- **Human Override Flow:**
    - Required justification text
    - Optional attachments stored in WORM artifacts
- **Notifications Upgraded:**
    - Assessment change triggers
    - Delay notifications and escalation events
- **Simulation API:**
    - What-if runs for policy validation and tuning

---

# Phase 6 — Hardening & Pilot Readiness

**Modules matured:** All

**Outcomes**

- **Branding & White-Label Readiness:**
    - Tenant branding (logos, colors, templates)
    - Policy snapshot UI contract for tenant-managed policies
    - Theming support across Holmes.Client
- **Observability:**
    - Dashboards for SLA health, adverse-action throughput, adjudication performance
    - Metrics + tracing coverage (OpenTelemetry)
- **Chaos & Property Testing:**
    - Duplicate event ingestion
    - Out-of-order delivery
    - SSE reconnect storms
- **Performance Tuning:**
    - Projection replay efficiency
    - Index optimization for MySQL
    - SSE scalability tuning
- **Deployment & Ops Maturity:**
    - Automated container image builds
    - Migration automation
    - Runbooks completed and validated

---

# Phase Summary (High-Level)

- **Phase 3:** Compliance foundations (SLA, policy, PP, notifications, IdP federation)
- **Phase 4:** Full Adverse Action + Evidence Packs + WORM storage
- **Phase 5:** Adjudication Engine with rulesets, overrides, and simulation
- **Phase 6:** Hardening, white-label readiness, observability, and pilot launch

This updated PLAN.md aligns the roadmap with the Compliance Suite architecture, monetizable boundaries, and the new
Identity Broker model.

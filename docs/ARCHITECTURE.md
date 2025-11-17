# Holmes — Intake · Workflow · SLA · Audit · Compliance · Adjudication

**Design Document (v1)**  
**Date:** November 5, 2025  
**Author:** Prepared for: Heath (Software Architect)

---

## 1) Executive Summary

Build a mobile-first **intake and workflow core** for background screening that is:

- A **Subject-first system** (one person → one canonical record).
- An explicit **state machine** with visible **SLA** and **regulatory clocks** (no timers hidden in sagas).
- **Event-sourced** with **CQRS** read models for instant visibility and audit.
- **Compliance-by-construction** (FCRA/EEOC/613/611, Fair-Chance overlays, CA ICRAA) with immutable evidence packs.
- **Adjudication matrices** that are explainable, policy-as-data, and fair-chance aware.
- Integrations are **adapters** behind an anti-corruption layer (stubs in v1).

**Non-goals (v1):** deep provider automations, full pricing/billing engine, postal letters (email/SMS only).

---

## 2) Scope & Objectives

**Primary objectives**

- Sub-minute intake from invite → submit (P50), P90 < 24 hours.
- Queryable **SLA** and **pre-adverse/adverse** clocks; breach alerts.
- Immutable **audit ledger** and **Timeline** per Order/Subject.
- **Notifications** with policy-driven rules across email/SMS/webhooks.
- **Adjudication** decisions that are deterministic, explainable, and human-conferrable.
- **Policy snapshots**: configuration, not per-client forks in code.

**Out-of-scope (v1)**: Full court/drug/MVR automation (use stubs), multi-region residency (design-ready, v2).

---

## 3) Architecture Overview

**Flow:**  
ATS/HRIS/PM → **Intake API** → **Orchestrator** → **Provider Adapters (stubs)** → **Data Normalization** → *
*Adjudication** → **Adverse Action/Disputes** → **Reporting/Billing**  
↘ **Consent/IDV** ↙ ↘ **Ledger/Audit** ↙

> **Note on the MCP dev sidecar:** The original v1 plan called for a developer-only MCP host that exposes tooling
> endpoints. That effort is now explicitly deferred to the backlog while Phase 1.9 hardening and Phase 2 workflow
> features take priority. Requirements (tool discovery, auth, orchestration) are preserved but the executable project is
> not expected to exist until after Phase 2.

**Principles**

- **DDD** with bounded contexts; **CQRS + Event Sourcing**.
- **Events** are the source of truth; read models provide instant visibility.
- **PII minimization** & field-level encryption; immutable WORM artifacts.

### Security · Audit · Compliance Doctrine

- Every aggregate mutation **must** emit an `EventRecord` (
  `src/Modules/Core/Holmes.Core.Infrastructure.Sql/Entities/EventRecord.cs`). The ledger cannot have gaps, alternate
  pathways, or mutable history.
- Tenant isolation is absolute: event payloads, snapshots, read models, and caches never blend customer data, and
  processors can only operate inside the tenant context carried by the initiating command.
- PII is minimized and encrypted at rest (field-level when possible) and is only exposed through authorized read models;
  ephemeral caches stay PII-free.
- Compliance is a product feature: flows must remain explainable, time-stamped, and reproducible so Holmes sustains *
  *100%** audit readiness for FCRA/EEOC/ICRAA plus customer overlays.
- Authentication remains invite-only. First authenticated requests invoke `RegisterExternalUserCommand` via
  `ICurrentUserInitializer`; if no Holmes user exists for the email, a `UninvitedExternalLoginAttempted` domain event is
  published, the attempt is logged, and middleware clears the cookie + redirects to `/auth/access-denied`.
- Invited users move directly from `Invited` to `Active` on their first successful login. There is
  no manual approval queue—either you were invited and can sign in, or the attempt is rejected and recorded.

### Users Module (Phase 1 preview)

**Bounded context goal:** project external OIDC identities into Holmes, capture authorization roles, and expose
tenant-scoped policies without ever persisting credentials.

- **Aggregates**
    - `UserAggregate`: canonical record keyed by `UlidId` with external identity tuple `(issuer, subject)`, profile (
      email, name), status (`Invited`, `Active`, `Suspended`), and `RoleAssignments`.
    - `RoleAssignment` value object: `Role` (`Admin`, `CustomerAdmin`, `Ops`, `Auditor`, etc.), optional `CustomerId`,
      `GrantedBy`, `GrantedAt`.
    - `ExternalIdentity` value object: `Issuer`, `Subject`, `AuthenticationMethod`, `LinkedAt`, `LastSeenAt`.
- **Domain events**
    - `UserRegistered` – first successful trust of an external token.
    - `UserProfileUpdated` – profile attribute refresh from IdP.
    - `UserRoleGranted` / `UserRoleRevoked`.
    - `UserSuspended` / `UserReactivated`.
- **Commands**
    - `RegisterExternalUserCommand` – invoked after token validation; idempotent on `(issuer, subject)`.
    - `GrantUserRoleCommand` / `RevokeUserRoleCommand` – enforce tenant scope and invariants (e.g., cannot remove last
      global Admin).
    - `UpdateUserProfileCommand` – keep audit trail of claim refreshes.
    - `SuspendUserCommand` / `ReactivateUserCommand`.
- **Policies**
    - `RequireGlobalRolePolicy(role)` – ensures caller holds global role via read model lookup.
    - `RequireCustomerRolePolicy(role, customerId)` – ensures caller is assigned role scoped to target customer.
    - Invariants like “a user cannot hold both Admin and CustomerAdmin for different tenants” expressed via aggregate
      guards.
- **Read models**
    - `user_directory` – flattened profile plus `Issuer`, `Subject`, last seen, statuses for quick lookup & API
      responses.
    - `user_role_memberships` – per `(userId, role, customerId)` rows to power authorization checks and UI.
    - `user_login_audit` – append-only event projection capturing login timestamps from `UserRegistered` +
      `UserProfileUpdated`.
- **Application services**
    - `OidcLoginHandler` – orchestrator that receives validated tokens, runs `RegisterExternalUserCommand`, and stamps
      correlation ids.
    - `RoleAssignmentService` – higher-level orchestration for Grant/Revoke with domain-based validation.
    - `AuthorizationCacheRefresher` – updates distributed cache entries when role events fire.
- **Integration points**
    - `IOidcProviderCatalog` – resolves customer-specific OIDC settings (issuer, audience) to validate tokens before
      commands execute.
    - API endpoints: `/api/users/me` (introspect), `/api/admin/users`, `/api/customers/{id}/admins`.
- **Testing**
    - Unit tests for aggregate invariants (prevent duplicate roles, protect last admin).
    - Integration tests asserting role-based authorization flows (e.g., CustomerAdmin cannot grant global Admin) via
      header-driven test auth over `WebApplicationFactory`.
    - Projection consistency tests – replays should rebuild `user_directory` exactly.

This module sits orthogonally to authentication middleware: the HTTP pipeline validates access tokens, then Application
handlers consult the read models to enforce policies.

### Solution Layout & Layering

- `Holmes.sln` ties all .NET projects (hosts, modules, tooling) into one solution.
- `docker-compose.yml` + scripts (e.g. `ef-reset.ps1`) support local infra and database resets.
- `global.json` and `nuget.config` pin the .NET SDK and package feeds.
- `src/` is the single home for runtime hosts, modules, client, and supporting tests.

#### Module Conventions

Every bounded context ships the same three projects so dependencies remain predictable:

| Project                               | Responsibilities                                            | References                                           |
|---------------------------------------|-------------------------------------------------------------|------------------------------------------------------|
| `Holmes.<Feature>.Domain`             | Aggregates, domain events, `I<Feature>UnitOfWork`, policies | `Holmes.Core.Domain`                                 |
| `Holmes.<Feature>.Application`        | Commands, queries, handlers, DTOs                           | `<Feature>.Domain`, `Holmes.Core.Application`        |
| `Holmes.<Feature>.Infrastructure.Sql` | DbContext, repositories, UnitOfWork, DI helpers             | `<Feature>.Domain`, `Holmes.Core.Infrastructure.Sql` |

Additional infrastructure (e.g., caching, queues) follows the same naming pattern
(`Infrastructure.Redis`, `Infrastructure.Search`, etc.) but **never** references the Application layer.

Each Infrastructure project must expose a single `DependencyInjection` entry point:

```csharp
public static IServiceCollection Add<Feature>InfrastructureSql(
    this IServiceCollection services,
    string connectionString,
    ServerVersion version)
{
    services.AddDbContext<<Feature>DbContext>(options =>
        options.UseMySql(connectionString, version));

    services.AddScoped<I<Feature>UnitOfWork, <Feature>UnitOfWork>();
    return services;
}
```

The solution has a dedicated template in `docs/MODULE_TEMPLATE.md` that walks through the scaffolding steps
(folder layout, project references, DI wiring, and unit-of-work expectations). New feature slices **must** follow that
guide so they plug into `HostingExtensions.AddInfrastructure` without ad-hoc code.

> **Repository access rule:** repository interfaces are intentionally not registered in DI. Application services must
> request their module's unit of work (e.g., `IUsersUnitOfWork`) and reach repositories or directories through the
> exposed properties (`unitOfWork.Users`). This keeps every mutation inside the transaction boundary enforced by the
> unit of work and prevents stray repository usage.

### Unit of Work & Domain Events

Aggregates inherit from `AggregateRoot`, which implements `IHasDomainEvents` and automatically registers itself with an
ambient tracker whenever `AddDomainEvent` is invoked. The base `UnitOfWork<TContext>` drains that tracker immediately
after a successful `SaveChangesAsync()` call, publishes each notification via MediatR, and only then clears the
aggregate’s pending events. If the transaction fails, nothing is dispatched and the tracker retains the aggregate so a
re-run will replay the events.

**Usage pattern**

1. Aggregate mutates state and calls `AddDomainEvent(..)` (usually via a local `Emit` helper).
2. Repository persists the aggregate as usual—no explicit registration step required.
3. Command handler invokes `*UnitOfWork.SaveChangesAsync()`; when the commit succeeds the unit of work publishes the
   captured events.
4. `ClearDomainEvents()` runs automatically, resetting aggregates for subsequent mutations.

Because registration happens inside the aggregate base class, repositories cannot forget to participate in the event
pipeline and no additional queueing infrastructure is necessary.

> See `Holmes.Core.Tests/UnitOfWorkDomainEventsTests` for integration coverage of
> multi-aggregate commits and failure paths. Future modules should add similar
> tests whenever they extend the UnitOfWork abstraction.

### Client Application (Holmes.App)

- React + Vite (TypeScript) solution tracked via `Holmes.App.esproj`, launched with `npm run dev`.
- `Holmes.App.Server` references the SPA via `SpaRoot` + SpaProxy so `dotnet run` proxies to `https://localhost:3000`
  during development and serves the static build in production pipelines.
- Phase 1.5 delivers an admin-focused shell that surfaces Subject, User, and Customer read models, plus flows to invite
  and activate users, grant/revoke roles, create customers, and inspect deduped subjects—proving Phase 1 behavior via
  UI.
- Shared API clients (OpenAPI-generated or typed fetch helpers) live beside the SPA to keep DTOs aligned with server
  contracts; basic Playwright/component tests exercise the invite → activate → assign role path end-to-end.
- **UI architecture guardrails (Phase 1.9):**
    - Stay within the existing stack: React 19, React Router 6, MUI 7, Emotion, and `@tanstack/react-query` for data
      access. New dependencies require justification (e.g., accessibility tooling) and should be rare.
    - Establish design tokens (spacing, typography, semantic colors) and feed them through MUI's theme so shared
      components (app shell, SLA chips, role badges) remain consistent.
    - Route layout per bounded context (`/users`, `/customers`, `/subjects`) with shared scaffolds for navigation,
      filters, and audit panels; each layout owns its glossary-driven copy so terminology never drifts.
    - React Query hooks encapsulate server contracts (`useSubjectsDirectory`, `useCustomerAdmins`, etc.) and expose
      typed
      DTOs. Mutations centralize optimistic updates + toast/audit behaviors rather than duplicating `fetch`.
    - Interaction primitives (timeline, SLA badge, subject identity card) are built once in `src/components` and reused
      so UX partners can restyle them without chasing bespoke implementations.
    - Documented workshop outputs with Luis Mendoza + Rebecca’s UX partners specify IA diagrams, component inventories,
      and testing expectations (lint/format, `npm run build`, visual regression hooks) before Phase 2 features begin.
- Deliverables and conventions live in `docs/Holmes.App.UI.md`.

### Identity & Development Seeds

- `Holmes.Identity.Server` hosts a minimal Duende IdentityServer for development. Run
  `dotnet run --project src/Holmes.Identity.Server` (defaults to `https://localhost:6001`) before launching the main
  host. Dev credentials: `admin` / `password`. This project is explicitly local-only and never deployed past a
  developer workstation; production/staging environments point the Holmes host at real customer IdPs.
- Holmes.App.Server intercepts any unauthenticated HTML navigation and redirects to `/auth/options`, which renders the
  provider list directly on the server (including sanitized `returnUrl` handling) before handing off to the configured
  OpenID Connect challenge.
- The React app no longer duplicates that UI: `AuthBoundary` verifies `/users/me` once, and on any 401/403/404 it
  performs
  a full-page navigation back to `/auth/options?returnUrl=…`, ensuring session refreshes follow the same hardened flow
  as
  first-time sign-ins.
- Holmes.App.Server registers two baseline policies:
    - `AuthorizationPolicies.RequireAdmin` → requires the `Admin` role claim.
    - `AuthorizationPolicies.RequireOps` → requires `Ops` or `Admin`.
- A development-only hosted service (`DevelopmentDataSeeder`) mirrors the IdP user inside Holmes, grants the Admin role
  via domain commands, and seeds a demo customer with that admin assigned. This keeps the invite/role/customer flows
  runnable immediately after `dotnet run`.

**Layering rules**

- `Holmes.Core.*` is the kernel module; all feature modules depend on it for primitives, cross-cutting behaviors, and
  shared integrations.
- A module's `*.Domain` project is pure (no Infrastructure or Application dependencies) and may depend only on
  `Holmes.Core.Domain`.
- `*.Application` projects depend on their matching `*.Domain` + `Holmes.Core.Application`; they expose commands,
  queries, and pipelines for the host.
- `*.Infrastructure` projects depend on `*.Domain` + `Holmes.Core.Infrastructure.*`; they never reference the module's
  `*.Application`, and `*.Application` cannot reference `*.Infrastructure`.
- Host projects (`Holmes.App.Server`, `Holmes.Mcp.Server`) wire modules through DI by referencing each module's
  `*.Application` and `*.Infrastructure`.
- Build outputs (`bin/`, `obj/`) stay inside each project and remain git-ignored.

---

## 4) Ubiquitous Language (DDD)

- **Subject** — Person being screened (canonical identity; dedup/merge capable).
- **Order** — Screening request for a Subject under a package and policy snapshot.
- **Product** — Unit of work (criminal, MVR, TWN); abstracted behind adapters.
- **Consent** — Signed authorization/disclosure artifacts tied to an Order.
- **Clock** — SLA or regulatory timer with business-day math and deadlines.
- **Policy** — Versioned configuration (tenant/client/role/jurisdiction overlays).
- **Notice** — Pre-adverse/final communications + delivery proofs.
- **Timeline** — Ordered, immutable events for audit and UI.
- **Assessment** — Adjudication evaluation and outcome for an Order.

---

## 5) Bounded Contexts

1) **Core Kernel** — Shared primitives, integration events, pipeline behaviors, crypto helpers.
2) **Subjects** — Canonical identity, aliases, merges, lineage.
3) **Users** — Operator accounts, roles, audit actors, tenant membership.
4) **Customers** — CRA client organizations, policy mapping, billing profile, contacts.
5) **Intake** — Invites, OTP verification, consents, PII capture, optional IDV.
6) **Order Workflow** — State machines, transitions, package routing (abstract).
7) **SLA/Clocks** — Business calendars, deadlines, at-risk/breach detection.
8) **Compliance Policy** — FCRA/EEOC/613/611, Fair-Chance, ICRAA, DOT overlays, policy packs.
9) **Notifications** — Rules, channels (email/SMS/webhook), delivery proofs.
10) **Adverse Action** — Two-step process, notices, evidence packs, disputes integration.
11) **Adjudication** — RuleSets, Assessments, Charge Taxonomy, human-in-the-loop.
12) **Audit/Ledger** — Event store, WORM artifacts, projections.
13) **Provider Adapters** — Anti-corruption layer; stubs for v1.

---

## 6) Aggregates & Invariants (selected)

### Subject (Root)

- One canonical record per person. Aliases allowed; merges preserve lineage.
- PII encrypted; SSN tokenized.  
  **Events:** Subject.Registered, Subject.AliasAdded, Subject.Merged.

### User (Root)

- Unique login per tenant; may belong to multiple tenants with scoped roles.
- Immutable audit identity; status gates (invited, active, suspended, disabled).  
  **Events:** User.Invited, User.Activated, User.RolesUpdated, User.Suspended, User.Disabled.

### Customer (Root)

- Represents a CRA client; binds policy packs, billing profile, contact roster.
- Must have an owning tenant; may define sub-locations/jurisdictions for routing.  
  **Events:** Customer.Registered, Customer.PolicySnapshotAssigned, Customer.ContactUpdated, Customer.Suspended.

### Order (Root)

- States:
  `created → invited → intake_in_progress → intake_complete → ready_for_routing → in_progress → ready_for_report → closed`.
- Must bind a Subject and a **policy_snapshot_id**.  
  **Events:** Order.Created, Invite.Sent, Consent.Captured, Intake.Submitted, Order.StateChanged, Order.Canceled.

### Clock (Root; SLA & Adverse)

- Deterministic deadlines with business-day math; pausable; visible index.  
  **Events:** Clock.Started, Clock.AtRisk, Clock.Breached, Clock.Paused, Clock.Resumed, Clock.ReadyToAdvance.

### Notice (Entity under Adverse)

- Template ids + render hashes; delivery proofs; artifacts in WORM.  
  **Events:** Notice.Prepared, Notice.Sent, Notice.DeliveryFailed, Notice.Delivered.

### Assessment (Adjudication Root)

- States: `prepared → recommended → under_review → finalized`.  
  **Events:** Assessment.Prepared, Assessment.Recommended, Assessment.Overridden, Assessment.Finalized.

---

## 7) State Machines

### Order

```
created → invited → intake_in_progress → intake_complete → ready_for_routing → in_progress → ready_for_report → closed
```

**Guards**: `intake_complete` requires `Consent.Captured`; `ready_for_routing` requires policy/subject bound.

### Adverse Action

```
idle → pre_sent → [paused ←→ pre_sent] → ready_final → final_sent → closed
```

### SLA clocks (examples)

- **Intake SLA**: `invited → intake_complete` ≤ X business hours.
- **Routing SLA**: `intake_complete → ready_for_routing` ≤ Y business hours.
- **Overall SLA**: `created → ready_for_report` ≤ Z days (read-only v1).

---

## 8) Policies as Data (versioned)

Snapshot the exact policy at Order creation; store its id forever.

```yaml
id: pol_cli_acme_2025_11_01
intake:
  require_idv: true
  ssn_full: false
  address_years: 7
sla:
  intake_hours: 4
  routing_hours: 2
adverse:
  start_on: sent        # or delivered
  wait_business_days: 5
notifications:
  on_state:
    - when: order.invited
      channels: [email, sms]
      to: [subject]
      template: invite_v2
    - when: clock.at_risk
      channels: [email, webhook]
      to: [client_ops]
      template: sla_at_risk_v1
locales: { default: en-US }
branding: { logo_url: https://cdn/acme/logo.svg }
```

---

## 9) Clocks & Business-Day Math

A dedicated **BusinessCalendar** service provides:

- `add_business_days(ts, n, jurisdictions[]) -> deadline_ts`
- `diff_business_seconds(a, b, jurisdictions[]) -> seconds`

**Clock Index (read model)** — always queryable:

-

`adverse_action_clocks(clock_id, order_id, subject_id, client_id, state, pre_sent_at, deadline_at, remaining_business_s, pause_reason, jurisdictions, delivery_proofs_json, policy_snapshot_id, sla_status, created_at, updated_at)`

- `sla_clocks(clock_id, order_id, kind, state, started_at, deadline_at, sla_status, created_at, updated_at)`

**Watchdog** flags `on_track / at_risk / breached` for dashboards & alerts.

---

## 10) Notifications

- Channels: email, SMS, webhook (postal in v2).
- Provider abstraction; every send emits `Notification.Sent` (with provider ids).
- Throttle/dedupe; retries with exponential backoff.
- Delivery failures trigger `Notice.DeliveryFailed` and can **pause** adverse clocks per policy.

---

## 11) Timeline & Audit

- **Event Store** (append-only) is canonical; outbox publishes to broker (future).
- **Timeline projection** composes domain events + artifact refs for applicant/client/ops views.
- **WORM Artifacts**: consents, notices, renders, signatures; events carry **hashes**, binaries stored with object-lock.
- **Export**: Evidence packs (zip of PDFs + JSON) per order/date-range for audits/subpoenas.

---

## 12) Compliance-by-Construction (Integrated)

### Non-negotiable outcomes

- **Permissible Purpose (PP)** certification on every order.
- **Standalone FCRA disclosure + authorization** before screening; CA ICRAA overlays when applicable.
- **Two-step adverse action** with report copy + current CFPB Summary of Rights; visible, queryable clocks.
- **§607(b) Accuracy** & **§613 strict-procedures/notice** path for public records; record match fields.
- **§611 Disputes/Reinvestigation**: 30 days (+15 with new info); final adverse paused while open.
- **Seven-year reporting windows** for non-convictions; do not restart clocks.
- **Fair-Chance** (NYC/LA/SF/CA) gates + individualized assessment templates.
- **DOT Part 40** kept separate; MRO artifacts if enabled.
- **PBSA mapping** to speed accreditation audits.

### Compliance Policy Packs (policy-as-data)

- **Federal Baseline**: FCRA/EEOC/CFPB forms; pre-adverse/final flows.
- **NYC FCA / LA County FCO / SF FCO**: post-offer sequencing + clocks + forms.
- **CA ICRAA**: extra disclosures and CA summary of rights.
- **DOT** (optional): MRO flow hooks; confidentiality.

### Workflow Gates

- `created → invited` requires PP grant.
- `intake_in_progress → intake_complete` requires DisclosurePack acceptance.
- `ready_for_report` requires §613 strict-procedures pass or consumer notice proof.
- `pre_adverse` requires report + Summary of Rights artifacts attached.
- Criminal components blocked pre-offer in covered jurisdictions.

### Evidence Packs (immutable)

- **Disclosure & Auth Pack** (FCRA + ICRAA as applicable).
- **§613 Pack** (strict-procedures calc or consumer notice).
- **Pre-Adverse Pack** (report, Summary of Rights, clock metadata).
- **Final Adverse Pack** (final letter, CRA contact, dispute info).

### Compliance SLOs

- % orders with valid PP grant; disclosure defects; correct Summary-of-Rights version rate; 611 timeliness; Fair-Chance
  deadline adherence; §613 path usage.

---

## 13) Adjudication Matrices (Integrated)

### Design Goals

- **Explainable** outcomes with reason codes & record lineage.
- **RuleSet as data** (versioned snapshots; deterministic).
- **Fair-Chance aware**; legal gates before evaluation.
- **Human-in-the-loop** overrides with justification & attachments.

### RuleSet DSL (excerpt)

```yaml
id: rs_cli_acme_finance_2025_11_01
defaults: { outcome: clear, lookback_years_default: 7, arrest_only_excluded: true }
criteria:
  - id: CRIT_FINANCIAL_FELONY_7Y
    when:
      any:
        - charge.category in [financial, theft_fraud]
          and disposition.verdict in [CONVICTED, PLED]
          and charge.severity == FELONY
          and time_since.most_relevant_years <= 7
    outcome: review
    reason_code: RC-FINANCIAL-LOOKBACK
```

Outputs include `recommended_outcome`, matched criteria, excluded records (and why), taxonomy & rule versions.

### Charge Taxonomy

- Rule-based + curated statute map; versioned; curation queue for low-confidence mappings.

### Events & Read Models

- `RuleSet.Published`, `Assessment.Recommended`, `Assessment.Overridden`, `Assessment.Finalized`.
- `adjudication_queue`, `assessment_summary`, `matrix_impact_report` projections.

### Simulator & Analytics

- What-if simulations on historical normalized results; impact by role/jurisdiction; top ReasonCodes.

---

## 14) API Surface (OpenAPI sketches)

### Orders & Intake

- `POST /orders` — create order
- `POST /orders/{id}/invites` — send magic-link (sms/email)
- `POST /intake/sessions/{sid}/verify` — OTP
- `POST /intake/sessions/{sid}/consents` — capture consent (artifact)
- `POST /intake/sessions/{sid}/submit` — finalize intake
- `POST /orders/{id}/advance` — controlled state transitions

### Clocks & Timeline

- `GET /clocks/adverse/{order_id}` — visible regulatory clock
- `GET /clocks/sla?order_id=&kind=` — query SLA clocks
- `GET /timeline/{order_id}` — auditable event stream

### Compliance

- `POST /compliance/permissible-purpose` — certify PP
- `GET/POST /compliance/policies` — list/preview policy packs
- `POST /compliance/613/check` — strict-procedures vs notice evaluation
- `POST /compliance/fair-chance/{order_id}/start` — jurisdictional flow
- `POST/PATCH /disputes` — open/update disputes
- `GET /evidence-packs/{order_id}/{type}` — WORM bundle

### Adjudication

- `GET/POST /adjudication/rulesets` — authoring & publish
- `POST /adjudication/evaluate` — run engine for an order
- `POST /adjudication/assessments/{id}/override` — human override
- `POST /adjudication/assessments/{id}/finalize` — freeze outcome

### Webhooks we send

- `order.created`, `invite.sent`, `intake.submitted`, `order.ready_for_routing`
- `clock.at_risk`, `clock.breached`, `pre_adverse.sent`, `final_adverse.sent`
- `notice.delivery_failed`, `assessment.recommended`, `assessment.finalized`

---

## 15) Data Model (storage sketch)

### Write (per context)

- `subjects`, `subject_aliases`, `subject_links`
- `orders`, `order_policy_snapshots`
- `consents` (render_hash, doc_version, signed_at, artifact_ref)
- `clocks` (aggregate snapshots) + `events_outbox` (idempotent)
- `rulesets`, `assessments`, `assessment_matches`
- Compliance: `pp_grants`, `disclosure_acceptances`, `fair_chance_clocks`, `section613_controls`

### Read

- `order_summary`, `order_timeline_events`
- `adverse_action_clocks`, `sla_clocks`
- `adjudication_queue`, `assessment_summary`
- `notifications_history`

### Artifacts (WORM)

- `/artifacts/{order_id}/{type}/{hash}`

### SQL Starters (excerpt)

```sql
create table events_outbox(
  id char(26) primary key,
  aggregate_id varchar(64),
  aggregate_type varchar(64),
  event_type varchar(128),
  payload json,
  occurred_at datetime(6) default current_timestamp(6),
  published boolean default false,
  key idx_agg (aggregate_id)
);

create table adverse_action_clocks(
  clock_id char(26) primary key,
  order_id char(26),
  subject_id char(26),
  client_id varchar(64),
  state varchar(32),
  pre_sent_at datetime(6),
  deadline_at datetime(6),
  remaining_business_s bigint,
  pause_reason varchar(64),
  jurisdictions json,
  delivery_proofs json,
  policy_snapshot_id varchar(64),
  sla_status varchar(16),
  created_at datetime(6) default current_timestamp(6),
  updated_at datetime(6) default current_timestamp(6) on update current_timestamp(6),
  key idx_order (order_id)
);
```

---

## 16) Security & Privacy

- Field-level **AEAD encryption** for PII; SSN tokenization; least-privilege RBAC/ABAC.
- **PII minimization** in read models; artifact hashes in events (not raw).
- Object-lock (WORM) for evidence bundles; tamper-evident logs.
- Secrets vaulted; rotation policy; environment isolation.

---

## 17) Observability

- **Metrics**: invite→submit, intake P50/P90, on_track/at_risk/breached counts, notification send/fail, §611 dispute
  cycle time, assessment distribution.
- **Tracing**: command→events→projections (OpenTelemetry-style activity IDs).
- **Dashboards**: At-Risk Clocks, Breaches by Client, Intake Funnel, Assessment Queue.
- **Alerts**: SLA breaches, adverse-action deadline proximity, notification failure spikes.
- **Phase 1.9 action**: Projection + UnitOfWork instrumentation (latency, error rate, replay lag) and Grafana dashboards
  are mandatory exit criteria before Phase 2. Document the endpoints + dashboard URLs in DEV_SETUP.
- **Implementation**: `Holmes.App.Server` exposes a Prometheus scrape endpoint at `https://localhost:5001/metrics` and
  publishes OTLP traces when `OpenTelemetry:Exporter:Endpoint` (or `OpenTelemetry__Exporter__Endpoint` env var) is set.
  Metrics include runtime, ASP.NET Core, HttpClient, and `Holmes.UnitOfWork` histograms/counters.
- **Runbooks**: operational recipes (database reset, projection verification, observability hookup) live in
  `docs/RUNBOOKS.md` so onboarding devs and SREs have executable guidance.

---

## 18) Technology Baseline (swappable)

- Language: **.NET 8** (ASP.NET Core Minimal APIs + BackgroundService).
- DB: **MySQL 8** (InnoDB).
- Eventing: **MySQL event store** (append-only) + **SSE** change feed.
- Object storage: local dev filesystem (swap to S3/MinIO later).
- EF Core for domain data, configuration UIs, and migrations; direct SQL/Dapper reserved for the event store &
  projection hot paths.

---

## 19) Provider Adapters (v1 stubs)

- Every product call is an idempotent job with a stable request/response contract.
- Publish `Product.Requested → Product.Completed` (fixture payloads).
- Anti-corruption layer isolates upstream from vendor-specific semantics.

---

## 20) Testing Strategy

- **State machine** property tests: illegal transitions rejected.
- **Clock math**: holidays; pause/resume; recompute; at-risk → breached.
- **Compliance**: Disclosure correctness, Summary-of-Rights versioning, §613 strict-procedures vs notice, §611
  timelines, Fair-Chance gates.
- **Adjudication**: determinism; exclusions (arrest-only, stale non-convictions); rule/jurisdiction overrides; human
  override requirements.
- **Idempotency/chaos**: duplicate events, out-of-order deliveries, retries.
- **SSE**: resume with Last-Event-ID; multi-tenant isolation; throughput under burst.

---

## 21) Delivery Plan (high-level milestones)

### Phase 0 (Weeks 0–1): Bootstrap & Infrastructure

- `Holmes.App.Server` host skeleton with health check, Serilog, configuration layers.
- Holmes.Core primitives (`UlidId`, `Result<T>`, crypto stubs) and pipeline behaviors.
- Event store schema + EF Core migrations; projection runner harness.
- Docker Compose for MySQL; CI build + lint workflow.

**Acceptance**: Host boots behind Docker, runs migrations, and emits heartbeat telemetry.

### Phase 1 (Weeks 1–3): Identity & Tenancy Foundations

- Subject registry aggregates + merge flows; canonical identity read model.
- Users module (invite, activate, suspend) with tenant membership + role claims.
- Customers module (register, assign policy snapshot, contact roster) tied to tenants.
- Read models: `subject_summary`, `user_directory`, `customer_registry`.
- Integration tests for user activation, subject merge, and customer linkage.

**Acceptance**: Tenant admin can invite a user, activate, assign roles, and create a customer bound to policy snapshot.

### Phase 1.5 (Weeks 3–4): Platform Cohesion & Event Plumbing

- Align `Holmes.Core` conventions, base behaviors, and dependency rules with what the Users/Customers modules actually
  needed during Phase 1 so new feature slices inherit a consistent template.
- Lock down the shared `UnitOfWork<TContext>` + domain-event dispatch pipeline with integration tests (multi-aggregate
  commits, failure rollbacks), and document the usage inside this architecture guide.
- Wire the identity/tenant read models into `Holmes.App.Server` (auth middleware, authorization helpers, seed scripts)
  so the host is ready for Intake flows without code churn.
- Expand base observability (structured logs, correlation IDs, baseline metrics) and add dev ergonomics like database
  reset scripts and fixture seeds.

**Acceptance**: A developer can run the host, exercise user/customer flows end-to-end with domain events firing once per
transaction, and new modules can copy the hardened templates without manual tweaks.

### Phase 1.9 (Weeks 4–5): Foundation Hardening & UX Architecture

- Explicitly move the developer MCP sidecar into the backlog; Phase 2 planning assumes it is deferred until after
  workflow launch.
- Instrument projections + UnitOfWork with metrics/tracing and publish Grafana dashboards + alerts documenting
  projection lag, failure counts, and command latency.
- Stand up Subjects and Customers automated tests (unit + EF integration) and expand Users coverage; wire them into the
  CI `dotnet test` pipeline.
- Verify Subjects/Customers read models against the documented behaviors and capture the verification steps in
  DEV_SETUP; add runbooks for `ef-reset`, Dockerized MySQL resets, and projection replays.
- Run the Holmes.App architecture workshop with Luis Mendoza (design tokens, layout shells, React Query conventions)
  staying inside the current dependency set (React Router, MUI, React Query, Emotion).
- Engage Rebecca’s UX collaborators to produce the reusable UI primitives (timeline, SLA badge, audit panel, role badges
  and action rails) so CRUD placeholders are replaced before Workflow stories land.

**Acceptance**: Green `dotnet build`, `dotnet test`, `npm run lint:fix`, and `npm run build`; observability dashboards
shareable; UI architecture notes checked into `/docs`; readiness review signed off for Phase 2 go/no-go.

### Phase 2 (Weeks 4–7): Intake & Workflow Launch

- Intake sessions + order workflow aggregates with subject/customer linkage.
- REST + SSE endpoints for invite → submit, and order state transitions.
- `Holmes.App` PWA shell covering OTP verify, consent capture, intake submission.
- Projections: `order_summary`, `order_timeline_events`, `intake_sessions`.

**Acceptance**: End-to-end intake completes, order advances to `ready_for_routing`, and SSE streams events with
Last-Event-ID resume.

### Phase 3 (Weeks 7–9): SLA, Compliance & Notifications

- Business calendar service, SLA watchdog, regulatory/adverse clock aggregates.
- Compliance policy gating (PP grants, disclosure acceptance, fair-chance overlays).
- Notification rules v1 for email/SMS/webhook with history + retries.
- Read models: `sla_clocks`, `adverse_action_clocks`, `notifications_history`.

**Acceptance**: Intake SLA flips to at_risk/breached, compliance gates block unauthorized progression, notifications
fire with idempotent delivery.

### Phase 4 (Weeks 9–10): Adverse Action & Evidence Packs

- Adverse action state machine with pause/resume + dispute linkage.
- WORM artifact store + evidence pack bundler (zip + manifest).
- API endpoints for regulator view and audit download.

**Acceptance**: Pre/final adverse timelines recompute after pause, evidence pack download verifies hash manifest.

### Phase 5 (Weeks 10–12): Adjudication Engine

- Charge taxonomy ingestion + normalization for assessments.
- RuleSet authoring + publish workflow; deterministic recommendation engine.
- Reviewer queue read models, override workflow, enriched notifications/SSE.

**Acceptance**: Assessment recommendations return consistent reason codes, overrides require justification and emit
audit trail.

### Phase 6 (Weeks 12–13): Hardening & Pilot

- Tenant branding + locale hooks; policy snapshot UI contract finalized.
- SLA/adverse/adjudication dashboards; audit export; SLO tracking.
- Chaos/property tests (duplicate events, SSE reconnect storms); perf tuning.

**Acceptance**: Pilot tenant runs through intake→adjudication→adverse action with dashboards lit and SLOs green.

---

## 22) Marketability (review notes)

**Category:** Screening intake & workflow orchestration with compliance and explainable adjudication.  
**ICP:** Mid-market CRAs and high-volume in-house screeners on legacy OS stacks.  
**Differentiators:** Visible **Clock Index** and **Timeline**; policy snapshots (no code forks); evidence packs;
deterministic adjudication + simulator.  
**Pricing idea:** Platform fee + usage (orders) + “Compliance Pack” add-on.  
**Positioning:** “Provable compliance and explainable decisions, without ripping out your CRA OS.”

---

## 23) Appendices

### A. Event Contracts (excerpt)

- `Order.Created`, `Invite.Sent`, `Consent.Captured`, `Intake.Submitted`, `Order.StateChanged`,  
  `Clock.Started`, `Clock.AtRisk`, `Clock.Breached`, `Notice.Sent`, `Assessment.Recommended`, `Assessment.Finalized`.

### B. Example Requests (excerpt)

```json
{ "cmd":"CreateOrder","order_id":"ord_01","client_id":"cli_01",
  "subject_ref":{"seed":{"first_name":"Avery","last_name":"Nguyen","dob":"1993-07-04","ssn_last4":"1234"}},
  "package":"EMP_STD_US","policy_id":"pol_cli_acme_2025_11_01" }
```

```json
{ "evt":"Clock.Started","clock_id":"clk_01","kind":"adverse","order_id":"ord_01","deadline_at":"2025-11-12T10:30:00Z" }
```

- Database reset (EF migrations + schema drop/create) is handled by `ef-reset.ps1` in the repo root. Running it
  rebuilds the Core/Users/Customers/Subjects schemas, ensuring every dev starts from the same migrations baseline.

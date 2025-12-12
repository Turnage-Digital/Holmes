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
- **Read models (projections)**
    - `user_projections` – flattened profile plus `Issuer`, `Subject`, last seen, statuses for quick lookup & API
      responses. Updated via `UserProjectionHandler` event handler.
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

### Services Module (Phase 3.1)

**Bounded context goal:** Manage the lifecycle of background check service requests, orchestrate vendor integrations
via an anti-corruption layer, normalize results into a canonical schema, and provide visibility into fulfillment
progress.

**Why Services is a Separate Bounded Context:**

- Services have independent lifecycles (vendor callbacks, retries, completion at different times)
- Variety of service types with different semantics (criminal vs employment vs education)
- Need to scale independently from Order aggregate
- Anti-corruption layer isolates vendor-specific protocols
- SLA clocks can track individual services, not just overall order

**Service Types (non-exhaustive):**

- **Criminal:** Federal, Statewide, County, Municipal searches
- **Identity:** SSN Verification, Address Verification, OFAC/Sanctions
- **Employment:** Employment Verification (TWN, direct), Income Verification
- **Education:** Education Verification, Professional License
- **Driving:** MVR, CDL Verification
- **Credit:** Credit Check (soft pull), Credit Header
- **Drug:** Drug Screen, MRO Review
- **Other:** Reference Check, Civil Records, Healthcare Sanctions

- **Aggregates**
    - `ServiceRequest`: Individual service tied to an Order with state machine (pending → dispatched → in_progress →
      completed/failed/canceled), vendor assignment, result storage, and SLA tracking.
    - `ServiceCatalog`: Defines available service types, their configurations, and vendor mappings per customer.

- **Infrastructure Services**
    - `IVendorCredentialStore`: Retrieves encrypted vendor credentials (API keys, account IDs) at dispatch time.
      Implementation may use Key Vault, Secrets Manager, or encrypted database. Credentials are operational
      infrastructure, not domain aggregates.

- **Value Objects**
    - `ServiceType`: Enum with category (Criminal, Employment, etc.) and specific type (CountySearch, TWN, etc.).
    - `ServiceScope`: Geographic/jurisdictional scope (county FIPS, state, federal, international).
    - `ServiceTier`: Execution priority tier (1 = first, 2 = after tier 1 completes, etc.). Customer-defined.
    - `VendorAssignment`: Which vendor handles this request, with fallback chain.
    - `ServiceResult`: Normalized result with status, records found, hit/clear, raw response hash.
    - `NormalizedRecord`: Canonical record schema (criminal charge, employment history, etc.).

- **Service Tiering (Customer-Defined)**
    - Services execute in tiers to optimize cost and fail-fast on disqualifying results.
    - **Tier 1 (Identity/Validation):** SSN Trace, Address Verification, OFAC — cheap, fast, gates further work.
    - **Tier 2 (Core Searches):** Criminal (federal, state, county), Employment, Education — bulk of the work.
    - **Tier 3 (Expensive/Slow):** Drug Test, Physical, MVR, Credit — only if earlier tiers pass.
    - Tier boundaries are customer-configurable; some customers may run everything in parallel.
    - If a tier produces a "stop" result (e.g., SSN mismatch), subsequent tiers can be skipped or require review.
    - `ServiceCatalog` stores tier assignments per service type per customer.

- **Domain Events**
    - `ServiceRequestCreated` – request queued for dispatch.
    - `ServiceRequestDispatched` – sent to vendor.
    - `ServiceResultReceived` – raw result from vendor callback.
    - `ServiceRequestCompleted` – result normalized and ready.
    - `ServiceRequestFailed` – vendor error or timeout.
    - `ServiceRequestCanceled` – order canceled or service no longer needed.
    - `ServiceRequestRetried` – retry attempt after transient failure.

- **Commands**
    - `CreateServiceRequestCommand` – queue a new service (from Order routing).
    - `DispatchServiceRequestCommand` – send to vendor via adapter.
    - `RecordServiceResultCommand` – capture vendor callback.
    - `CompleteServiceRequestCommand` – mark as done after normalization.
    - `RetryServiceRequestCommand` – retry failed request.
    - `CancelServiceRequestCommand` – cancel pending/in-progress request.

- **Queries**
    - `GetServiceRequestsByOrderQuery` – all services for an order.
    - `GetPendingServiceRequestsQuery` – services awaiting dispatch.
    - `GetServiceRequestStatusQuery` – current status of a service.

- **Integration with Order Workflow**
    - When Order reaches `ReadyForRouting`, the `OrderRoutingService` determines which services are needed based on
      the package and policy, then emits `CreateServiceRequestCommand` for each.
    - When Order enters `RoutingInProgress`, individual services are dispatched.
    - Order transitions to `ReadyForReport` only when all required services are `Completed` (or explicitly waived).
    - `ServiceRequestCompleted` events trigger `CheckOrderReadyForReportCommand` to evaluate if Order can advance.

- **Integration with SLA Clocks**
    - Individual services can have their own SLA clocks (e.g., county search 3-day SLA).
    - Fulfillment clock on Order tracks aggregate service completion.
    - Service-level SLAs are optional and customer-defined.

- **Anti-Corruption Layer (Vendor Adapters)**
    - `IVendorAdapter` interface with `DispatchAsync`, `ParseCallbackAsync`, `GetStatusAsync`.
    - Each vendor has its own adapter implementation (e.g., `SterlingAdapter`, `GoodHireAdapter`).
    - Adapters translate between vendor-specific protocols and Holmes canonical schema.
    - Phase 3.1 uses `StubVendorAdapter` that returns fixture data after configurable delay.

- **Read Models**
    - `service_requests` – current state of all service requests.
    - `service_results` – normalized results for reporting.
    - `vendor_activity` – dispatch/callback audit trail.
    - `fulfillment_dashboard` – aggregate view of in-flight services.

- **Address History (Subject Enhancement)**
    - Subject module extended with `AddressHistory` collection.
    - Each `Address` has: street, city, state, postal, country, from_date, to_date, is_current.
    - Policy defines `address_years` lookback (e.g., 7 years) for county determination.
    - `GetCountiesForAddressHistoryQuery` returns FIPS codes for criminal searches.

### Solution Layout & Layering

- `Holmes.sln` ties all .NET projects (hosts, modules, tooling) into one solution.
- `docker-compose.yml` + scripts (e.g. `ef-reset.ps1`) support local infra and database resets.
- `global.json` and `nuget.config` pin the .NET SDK and package feeds.
- `src/` is the single home for runtime hosts, modules, client, and supporting tests.

#### Module Conventions

Every bounded context ships four projects to support CQRS and database swappability:

| Project                                     | Responsibilities                                                                             | References                                                                                 |
|---------------------------------------------|----------------------------------------------------------------------------------------------|--------------------------------------------------------------------------------------------|
| `Holmes.<Feature>.Domain`                   | Aggregates, domain events, `I<Feature>UnitOfWork`, write-focused repository interfaces       | `Holmes.Core.Domain`                                                                       |
| `Holmes.<Feature>.Application.Abstractions` | DTOs, query interfaces (`I<Feature>Queries`), broadcasters                                   | `<Feature>.Domain`, `Holmes.Core.Domain`                                                   |
| `Holmes.<Feature>.Application`              | Commands, MediatR query objects (`*Query` + handlers in `Queries/` folder), command handlers | `<Feature>.Domain`, `<Feature>.Application.Abstractions`, `Holmes.Core.Application`        |
| `Holmes.<Feature>.Infrastructure.Sql`       | DbContext, repositories, query implementations (`Sql<Feature>Queries`), Specifications       | `<Feature>.Domain`, `<Feature>.Application.Abstractions`, `Holmes.Core.Infrastructure.Sql` |

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

    // Write side
    services.AddScoped<I<Feature>UnitOfWork, <Feature>UnitOfWork>();

    // Read side (CQRS)
    services.AddScoped<I<Feature>Queries, Sql<Feature>Queries>();

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

### Event Persistence & Projections

Domain events are persisted to the `EventRecord` table during `SaveChangesAsync()` within the same database transaction
as the aggregate state changes. This ensures atomicity between state and events.

**Event Flow:**

1. Aggregate emits domain events via `AddDomainEvent()`
2. `UnitOfWork.SaveChangesAsync()` begins a transaction
3. Aggregate state is saved to the database
4. Events are serialized and written to `EventRecord` table via `IEventStore`
5. Transaction commits
6. Events are dispatched via MediatR to handlers (including projection handlers)

**Projection Architecture:**

Read models (projections) are updated via MediatR event handlers rather than synchronous writes in repositories. This
provides clear separation between write-side (aggregates) and read-side (projections) updates.

| Module    | Projection Table             | Handler                          | Events Handled                                         |
|-----------|------------------------------|----------------------------------|--------------------------------------------------------|
| Users     | `user_projections`           | `UserProjectionHandler`          | UserInvited, UserRegistered, UserProfileUpdated, etc.  |
| Customers | `customer_projections`       | `CustomerProjectionHandler`      | CustomerRegistered, CustomerRenamed, CustomerSuspended |
| Subjects  | `subject_projections`        | `SubjectProjectionHandler`       | SubjectRegistered, SubjectMerged, SubjectAliasAdded    |
| Workflow  | `order_summaries`            | `OrderSummaryHandler`            | Various Order events                                   |
| Intake    | `intake_session_projections` | `IntakeSessionProjectionHandler` | IntakeSession events                                   |

**Benefits:**

- Events are the source of truth and can be replayed to rebuild projections
- Projections can be rebuilt from event history at any time
- Clear audit trail of all state changes
- Consistent architecture across all modules

**Projection Writers:**

Each projection has a corresponding writer interface (e.g., `IUserProjectionWriter`, `ICustomerProjectionWriter`)
implemented in the Infrastructure layer. These writers handle the actual database operations for maintaining projection
tables.

### Query Pattern (CQRS Read Side)

Controllers access read data through MediatR Query objects that delegate to Query Interfaces. This ensures:

- Consistent MediatR pipeline for all operations (logging, validation, caching via behaviors)
- Database-agnostic query interfaces in Application.Abstractions
- Uniform controller injection (only `IMediator` + security interfaces)

**Query Flow:**

```
Controller -> IMediator.Send(Query) -> QueryHandler -> I*Queries -> Sql*Queries -> DbContext
```

**Query Naming Conventions:**

| Pattern            | Example                        |
|--------------------|--------------------------------|
| Single by ID       | `Get{Entity}ByIdQuery`         |
| Single by criteria | `Get{Entity}By{Criteria}Query` |
| List/paginated     | `List{Entities}Query`          |
| Existence check    | `Check{Entity}ExistsQuery`     |
| Specific property  | `Get{Entity}{Property}Query`   |
| Stats/aggregations | `Get{Entity}StatsQuery`        |

**Query Handler Pattern:**

Query handlers in `*.Application/Queries/` inject query interfaces from `*.Application.Abstractions/Queries/`
and delegate all database access. Query handlers contain no direct DbContext usage.

```csharp
public sealed record GetCustomerByIdQuery(string CustomerId) : RequestBase<Result<CustomerDetailDto>>;

public sealed class GetCustomerByIdQueryHandler(
    ICustomerQueries customerQueries
) : IRequestHandler<GetCustomerByIdQuery, Result<CustomerDetailDto>>
{
    public async Task<Result<CustomerDetailDto>> Handle(
        GetCustomerByIdQuery request,
        CancellationToken cancellationToken
    )
    {
        var customer = await customerQueries.GetByIdAsync(request.CustomerId, cancellationToken);
        if (customer is null)
        {
            return Result.Fail<CustomerDetailDto>($"Customer {request.CustomerId} not found");
        }
        return Result.Success(customer);
    }
}
```

**Controller Usage:**

Controllers inject only `IMediator` for queries (plus security interfaces like `ICurrentUserAccess`).
They never inject query interfaces directly.

```csharp
public sealed class CustomersController(
    IMediator mediator,
    ICurrentUserAccess currentUserAccess
) : ControllerBase
{
    [HttpGet("{customerId}")]
    public async Task<ActionResult<CustomerDetailDto>> GetCustomerById(
        string customerId, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetCustomerByIdQuery(customerId), cancellationToken);
        if (!result.IsSuccess) return NotFound();
        return Ok(result.Value);
    }
}
```

### Client Application (Holmes.Internal)

- React + Vite (TypeScript) solution tracked via `Holmes.Internal.esproj`, launched with `npm run dev`.
- `Holmes.Internal.Server` hosts the SPA (SpaProxy in development, static files in production). `Holmes.App.Server`
  now stays API/SSE-only.
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
- Intake SPA remains a separate project (`Holmes.Intake`) with a chrome-free wizard; it may share a thin `ui-core`
  package (tokens, form primitives, OTP/consent controls, fetch helpers) with Internal, but navigation/role concepts
  stay isolated.
- Deliverables and conventions live in `docs/Holmes.App.UI.md`.

### Identity & Development Seeds

- `Holmes.Identity.Server` is now a first-class Duende IdentityServer backed by ASP.NET Identity + EF stores (MySQL).
  It seeds IdentityServer config (clients/resources/grants) plus baseline admin/ops users, persists data-protection
  keys, and serves the standard Identity UI for login, confirm-email, and reset flows. Run
  `dotnet run --project src/Holmes.Identity.Server` (defaults to `https://localhost:6001`) before launching the main
  host. Seed credentials: `admin@holmes.dev` / `ChangeMe123!` (reset immediately).
- Holmes.App.Server uses standard cookie + OIDC middleware (no custom ensure/redirect middleware). `/auth/login`
  challenges against IdentityServer; `/auth/logout` signs out the cookie. Authorization still flows through Holmes
  role claims emitted by the IdP profile service.
- Invites create the ASP.NET Identity user up front (blank/temporary password) and send a confirmation/set-password
  link. First login requires confirmed email; no “create user on first request” middleware remains.
- Holmes.App.Server registers two baseline policies:
    - `AuthorizationPolicies.RequireAdmin` → requires the `Admin` role claim.
    - `AuthorizationPolicies.RequireOps` → requires `Ops` or `Admin`.
- A development-only hosted service (`DevelopmentDataSeeder`) still mirrors the IdP admin into Holmes roles and seeds a
  demo customer so invite/role/customer flows remain runnable after `dotnet run`.

**Layering rules**

- `Holmes.Core.*` is the kernel module; all feature modules depend on it for primitives, cross-cutting behaviors, and
  shared integrations.
- A module's `*.Domain` project is pure (no Infrastructure or Application dependencies) and may depend only on
  `Holmes.Core.Domain`. Repository interfaces here are **write-focused** (`GetByIdAsync`, `Add`, `Update`).
- Each module exposes `*.Application.Abstractions` for DTOs, **query interfaces** (`I<Feature>Queries`), projection
  contracts, broadcasters, and other ports the host or Infrastructure must consume. These assemblies depend on the
  module's `*.Domain` (and `Holmes.Core.Domain`) but contain no handlers.
- `*.Application` projects depend on their matching `*.Domain`, their module's `*.Application.Abstractions`, and
  `Holmes.Core.Application`; they expose commands and query handlers for the host. Query handlers delegate to
  `I<Feature>Queries` interfaces—they do not access DbContext directly.
- `*.Infrastructure.Sql` projects depend on `*.Domain`, the module's `*.Application.Abstractions`, and
  `Holmes.Core.Infrastructure.Sql`. They implement both repository interfaces (write side) and query interfaces
  (read side via `Sql<Feature>Queries`). They never reference the module's `*.Application`, and `*.Application` cannot
  reference `*.Infrastructure`. Cross-module calls flow only through other modules' `*.Application.Abstractions`.
- `Holmes.App.Infrastructure` references **only** `*.Application.Abstractions` projects—never `*.Infrastructure.Sql`.
  This ensures middleware and security code remain database-agnostic.
- Host projects (`Holmes.App.Server`) wire modules through DI by referencing each module's
  `*.Application` and `*.Infrastructure.Sql`.
- Controllers inject `IMediator` for all queries and commands—never query interfaces or DbContext directly.
  Security interfaces (`ICurrentUserAccess`, `ICurrentUserInitializer`) and special-case infrastructure
  (e.g., `IEventStore` for audit) are the only other allowed injections.
- Build outputs (`bin/`, `obj/`) stay inside each project and remain git-ignored.

**Cross-module boundary rule (CRITICAL)**

A module **MUST NEVER** directly reference another module's `*.Domain` or `*.Application` projects. This is a
fundamental DDD principle — bounded contexts communicate through explicit contracts, not internal implementation
details.

| Reference Type                                                    | Allowed? | Example                                      |
|-------------------------------------------------------------------|----------|----------------------------------------------|
| `ModuleA.Application` → `ModuleB.Domain`                          | ❌ NO     | SlaClocks.App → Workflow.Domain              |
| `ModuleA.Application` → `ModuleB.Application`                     | ❌ NO     | Services.App → Subjects.App                  |
| `ModuleA.Infrastructure.Sql` → `ModuleB.Infrastructure.Sql`       | ❌ NO     | Customers.Infra.Sql → Users.Infra.Sql        |
| `ModuleA.Application` → `ModuleB.Application.Abstractions`        | ✅ YES    | SlaClocks.App → Workflow.App.Abstractions    |
| `ModuleA.Infrastructure.Sql` → `ModuleB.Application.Abstractions` | ✅ YES    | Intake.Infra.Sql → Workflow.App.Abstractions |
| `App.Infrastructure` → `Module.Application.Abstractions`          | ✅ YES    | App.Infra → Users.App.Abstractions           |
| `App.Infrastructure` → `Module.Infrastructure.Sql`                | ❌ NO     | App.Infra → Users.Infra.Sql                  |

When a module needs types from another bounded context, the owning module must expose them via
`*.Application.Abstractions` (DTOs, interfaces, integration events). See `docs/MODULE_TEMPLATE.md` for detailed
guidance and examples.

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
2) **Subjects** — Canonical identity, aliases, address history, merges, lineage.
3) **Users** — Operator accounts, roles, audit actors, tenant membership.
4) **Customers** — CRA client organizations, policy mapping, billing profile, contacts.
5) **Intake** — Invites, OTP verification, consents, PII capture, optional IDV.
6) **Order Workflow** — State machines, transitions, package routing (abstract).
7) **Services** — Background check service requests, vendor orchestration, results normalization.
8) **SLA/Clocks** — Business calendars, deadlines, at-risk/breach detection.
9) **Compliance Policy** — FCRA/EEOC/613/611, Fair-Chance, ICRAA, DOT overlays, policy packs.
10) **Notifications** — Rules, channels (email/SMS/webhook), delivery proofs.
11) **Adverse Action** — Two-step process, notices, evidence packs, disputes integration.
12) **Adjudication** — RuleSets, Assessments, Charge Taxonomy, human-in-the-loop.
13) **Audit/Ledger** — Event store, WORM artifacts, projections.
14) **Provider Adapters** — Anti-corruption layer; stubs for v1.

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

### ServiceRequest (Root)

- Represents a single background check service (criminal search, employment verification, etc.) tied to an Order.
- States: `pending → dispatched → in_progress → completed | failed | canceled`.
- Must bind to an Order and specify a `ServiceType`.
- Results normalized into standard schema regardless of vendor.
  **Events:** ServiceRequest.Created, ServiceRequest.Dispatched, ServiceRequest.ResultReceived,
  ServiceRequest.Completed, ServiceRequest.Failed, ServiceRequest.Canceled.

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

### SLA Clocks

SLA clocks track time-bound obligations on Orders. Implemented in `Holmes.SlaClocks` module.

| Clock Kind      | Trigger States                       | Default Target  |
|-----------------|--------------------------------------|-----------------|
| **Intake**      | `Invited` → `IntakeComplete`         | 1 business day  |
| **Fulfillment** | `ReadyForRouting` → `ReadyForReport` | 3 business days |
| **Overall**     | `Created` → `Closed`                 | 5 business days |

**Terminology Note:** The Order state machine uses `ReadyForRouting`/`RoutingInProgress` internally, but SLA
tracking uses **Fulfillment** to describe service execution (court searches, verifications, etc.). Default
SLAs are fallbacks; actual targets come from customer service agreements.

Clock states: `Running` → `AtRisk` (80% threshold) → `Breached` / `Completed`. Clocks can be `Paused` (e.g.,
for disputes or holds) and `Resumed`.

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
  intake_days: 1         # business days
  fulfillment_days: 3    # business days
  overall_days: 5        # business days
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

## 16.1) Identity Architecture (Broker Model)

Holmes.Identity.Server operates as a **federated identity broker**, not just a development stub.

**Modes of Authentication:**

- **Mode A: Tenant uses external IdP** (Azure AD, Okta, generic OIDC)
    - Holmes.Identity redirects to upstream IdP
    - Normalizes external identity into Holmes User Registry

- **Mode B: Holmes-managed credentials** (for smaller tenants)
    - Local password-based authentication in Holmes.Identity
    - Still issues the same Holmes-standard tokens

**Why a Broker?**

- Uniform claims model across all tenants
- Holmes enforces TenantId, roles, and policies centrally
- Clean multi-tenant isolation
- White-label friendly authentication UX
- Supports mixed environments (some tenants bring IdP, others don't)

Holmes.Identity.Server is the **single IdP for Holmes.App and Holmes.Internal**, delegating upstream only as needed.

---

## 16.2) White-Label & Multi-Tenant Architecture

Holmes supports private-label deployments with:

- Tenant-specific branding metadata (logo, colors, templates)
- Themed login flows via Holmes.Identity
- Themed Holmes.Internal UI
- Tenant-scoped IdP configuration (BYO IdP)
- Per-tenant features and billing entitlements

**Deployment Options:**

- Row-level tenant isolation via `TenantId` (default)
- Separate schemas per bounded context
- Optional per-tenant databases for enterprise deployments
- Azure deployments via AKS with horizontal pod scaling

---

## 16.3) WORM Artifacts & Evidence Packs

The `IConsentArtifactStore` abstraction supports **write-once, immutable storage** for:

- Policy snapshots
- Consent signatures
- Adverse notices
- Dispute submissions
- Evidence bundles

**Implementation:**

- Hash verification for regulator integrity requirements
- Backed initially by encrypted MySQL BLOBs (swappable to Azure Blob Storage)
- Deterministic **Evidence Packs** (ZIP of PDFs + JSON manifest) containing:
    - Full event history relevant to an order
    - All artifacts pertaining to adverse action or disputes
    - JSON manifest for reproducibility

---

## 16.4) Usage Metering & Entitlements

A dedicated module records billable events via `ComplianceUsageRecord`:

| Event Type | Description |
|------------|-------------|
| `AdverseActionCaseCreated` | Pre/final AA workflow initiated |
| `EvidencePackGenerated` | Evidence bundle export |
| `NotificationRequested` | Email/SMS/webhook fired |
| `DisputeOpened` | Candidate dispute initiation |
| `SimulationRun` | Policy what-if request |

**Entitlements gate features:**

- Compliance Suite
- Adverse Action Automation
- Evidence Packs
- Dispute Portal
- Adjudication Engine

This allows Holmes to run multiple SaaS tiers in one deployment. See `docs/monetize/PRICING.md` for tier definitions.

---

## 17) Observability

- **Metrics**: invite→submit, intake P50/P90, on_track/at_risk/breached counts, notification send/fail, §611 dispute
  cycle time, assessment distribution.
- **Tracing**: command→events→projections (OpenTelemetry-style activity IDs).
- **Dashboards**: At-Risk Clocks, Breaches by Client, Intake Funnel, Assessment Queue.
- **Alerts**: SLA breaches, adverse-action deadline proximity, notification failure spikes.
- **Phase 1.9 action**: Projection + UnitOfWork instrumentation (latency, error rate, replay lag) and Grafana dashboards
  are mandatory exit criteria before Phase 2. Document the endpoints + dashboard URLs in DEV_SETUP.
- **Implementation**: `Holmes.App.Server` exposes a Prometheus scrape endpoint at `https://localhost:5000/metrics` and
  publishes OTLP traces when `OpenTelemetry:Exporter:Endpoint` (or `OpenTelemetry__Exporter__Endpoint` env var) is set.
  Metrics include runtime, ASP.NET Core, HttpClient, and `Holmes.UnitOfWork` histograms/counters.
- **Runbooks**: operational recipes (database reset, projection verification, observability hookup) live in
  `docs/RUNBOOKS.md` so onboarding devs and SREs have executable guidance.

---

## 18) Technology Baseline (swappable)

- Language: **.NET 9** (ASP.NET Core Minimal APIs + BackgroundService).
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
- Read models: `subject_projections`, `user_projections`, `customer_projections`.
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

### Phase 3 (Weeks 7–8): SLA Clocks & Notifications Foundation

- Business calendar service, SLA watchdog for order-level clocks.
- Notification rules v1 for email/SMS/webhook with history + retries.
- Read models: `sla_clocks`, `notifications_history`.

**Acceptance**: Intake/Fulfillment/Overall SLA clocks flip to at_risk/breached, notifications fire with idempotent
delivery.

### Phase 3.1 (Weeks 8–10): Services & Fulfillment

- `ServiceRequest` aggregate with state machine and vendor assignment.
- Service catalog with type taxonomy (Criminal, Employment, Education, etc.).
- Anti-corruption layer with `IVendorAdapter` and stub implementations.
- Address history on Subject for county determination.
- Order routing logic: package → service requests.
- Service-level SLA clocks (optional, customer-defined).
- Read models: `service_requests`, `service_results`, `fulfillment_dashboard`.

**Acceptance**: Order routes to services based on package, stub vendors return results, Order advances to ReadyForReport
when all services complete, service-level SLAs track individual components.

### Phase 3.2 (Weeks 10–11): Compliance & Policy Gates

- Compliance policy gating (PP grants, disclosure acceptance, fair-chance overlays).
- Regulatory/adverse clock aggregates for pre-adverse timing.
- Read models: `compliance_grants`, `adverse_action_clocks`.

**Acceptance**: Compliance gates block unauthorized progression, PP required for order creation, disclosure required
before intake completion.

### Phase 4 (Weeks 11–12): Adverse Action & Evidence Packs

- Adverse action state machine with pause/resume + dispute linkage.
- WORM artifact store + evidence pack bundler (zip + manifest).
- API endpoints for regulator view and audit download.

**Acceptance**: Pre/final adverse timelines recompute after pause, evidence pack download verifies hash manifest.

### Phase 5 (Weeks 12–14): Adjudication Engine

- Charge taxonomy ingestion + normalization for assessments.
- RuleSet authoring + publish workflow; deterministic recommendation engine.
- Reviewer queue read models, override workflow, enriched notifications/SSE.

**Acceptance**: Assessment recommendations return consistent reason codes, overrides require justification and emit
audit trail.

### Phase 6 (Weeks 14–15): Hardening & Pilot

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

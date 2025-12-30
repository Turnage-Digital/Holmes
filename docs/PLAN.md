# Holmes Delivery Plan (Current)

**Last Updated:** 2025-12-23

This roadmap focuses on what is implemented today and what remains in near-term delivery. Detailed architecture lives in
`docs/ARCHITECTURE.md`.

---

## 0) Baseline Constraints

- **Runtime:** .NET 9 (ASP.NET Core, background services)
- **Database:** MySQL 8 with EF Core migrations (Pomelo provider)
- **Identifiers:** ULID per aggregate; global `events.position` as `BIGINT`
- **Event Store:** Append-only `events` table + snapshots; projections checkpointed
- **Streaming:** Server-Sent Events via `/api/*/changes` endpoints
- **Security:** OIDC + cookies via Holmes.Identity.Server; tenant isolation; AEAD for PII
- **Module layering:** `Domain` -> `Contracts` -> `Application` -> `Infrastructure.Sql`

---

## 1) Solution Layout (Current)

```
/src
  Holmes.App.Server/                 # API host (controllers, SSE, background services)
  Holmes.App.Server.Tests/           # Host-level tests
  Holmes.App.Infrastructure/         # Infrastructure utilities (db-agnostic)
  Holmes.Identity.Server/            # Duende IdentityServer + ASP.NET Identity
  Holmes.Internal/                   # Internal SPA (admin/ops)
  Holmes.Internal.Server/            # SPA host
  Holmes.Intake/                     # Intake SPA (subject-facing)
  Holmes.Intake.Server/              # Intake SPA host
  Modules/
    Core/
      Holmes.Core.Domain/
      Holmes.Core.Application/
      Holmes.Core.Contracts/
      Holmes.Core.Infrastructure.Sql/
      Holmes.Core.Infrastructure.Security/
      Holmes.Core.Tests/
    Subjects/
      Holmes.Subjects.Domain/
      Holmes.Subjects.Application/
      Holmes.Subjects.Contracts/
      Holmes.Subjects.Infrastructure.Sql/
      Holmes.Subjects.Tests/
    Users/
      Holmes.Users.Domain/
      Holmes.Users.Application/
      Holmes.Users.Contracts/
      Holmes.Users.Infrastructure.Sql/
    Customers/
      Holmes.Customers.Domain/
      Holmes.Customers.Application/
      Holmes.Customers.Contracts/
      Holmes.Customers.Infrastructure.Sql/
    Intake/
      Holmes.IntakeSessions.Domain/
      Holmes.IntakeSessions.Application/
      Holmes.IntakeSessions.Contracts/
      Holmes.IntakeSessions.Infrastructure.Sql/
      Holmes.IntakeSessions.Tests/
    Workflow/
      Holmes.Orders.Domain/
      Holmes.Orders.Application/
      Holmes.Orders.Contracts/
      Holmes.Orders.Infrastructure.Sql/
    Services/
      Holmes.Services.Domain/
      Holmes.Services.Application/
      Holmes.Services.Contracts/
      Holmes.Services.Infrastructure.Sql/
      Holmes.Services.Tests/
    SlaClocks/
      Holmes.SlaClocks.Domain/
      Holmes.SlaClocks.Application/
      Holmes.SlaClocks.Contracts/
      Holmes.SlaClocks.Infrastructure.Sql/
      Holmes.SlaClocks.Tests/
    Notifications/
      Holmes.Notifications.Domain/
      Holmes.Notifications.Application/
      Holmes.Notifications.Contracts/
      Holmes.Notifications.Infrastructure.Sql/
      Holmes.Notifications.Tests/
```

---

## 2) Phases (Current Status)

### Phase 0-1.9 — Platform Foundations (Complete)

- Identity, tenancy, user/customer/subject foundations
- Event store + unit of work and projections
- Holmes.Identity.Server operational
- Internal SPA scaffolding and admin flows

### Phase 2 — Intake and Workflow (Complete)

- Intake sessions + consent + submission
- Workflow order lifecycle with timeline/summary projections
- Intake -> Workflow gateway integration

### Phase 3.0 — SLA Clocks and Notifications (Complete, needs UX verification)

- SLA clock aggregate + watchdog + projections
- Notification aggregate + processing service
- SSE for clocks and order changes

### Phase 3.1 — Services and Fulfillment (Complete, needs UX verification)

- Service request aggregate + catalog
- Order fulfillment and completion handlers
- SSE for services

### Phase 3.2 — Subject Data Expansion (Active)

- Expand Subject data collections
- Complete intake UI and submission wiring
- Add subject detail UX for address/employment/education

---

## 3) Near-Term Focus (Phase 3.2)

1. Expand Subject collections (address/employment/education/reference/phone) + EF migrations.
2. Wire Intake UI forms to persistence and submission.
3. Add county resolution service for address history.
4. Validate Holmes.Internal UI integration (services, clocks, notifications).
5. Wire SLA at-risk/breached events to Notification requests.

---

## 4) Future Phases (Planned, Not Implemented)

- Compliance policy packs and regulatory clocks
- Adverse action workflow + evidence packs
- Adjudication engine + charge taxonomy
- Billing, entitlements, and monetization

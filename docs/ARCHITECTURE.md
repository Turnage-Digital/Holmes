# Holmes Architecture (Current State)

**Last Updated:** 2025-12-23

This document reflects what is implemented today. It intentionally avoids future-phase promises; planned work is captured
in the backlog section at the end.

---

## 1) Executive Summary

Holmes is a modular, event-first workflow system for background screening intake and fulfillment. The current system
centers on Intake, Workflow, Services, SLA clocks, and Notifications running together behind a single API host.

**Today in scope**

- Subject-first data model with Orders as the workflow driver.
- Intake sessions with consent capture, submission, and acceptance.
- Order workflow state machine with SLA clocks and timeline/audit projections.
- Services fulfillment with service catalog and service request lifecycle.
- Notifications (email/SMS/webhook) driven by Intake/SLA triggers.
- CQRS read models and SSE for live updates.

**Explicitly out of scope (not implemented yet)**

- Adverse action workflow and evidence packs.
- Adjudication engine, charge taxonomy, and policy simulation.
- Compliance policy packs beyond current intake gating.
- Provider adapters beyond stubs and vendor orchestration.

---

## 2) System Overview

```
ATS/HRIS/PM -> Holmes.App.Server (API + SSE)
                     |
                     |-- Intake (invite, start, submit, accept)
                     |-- Workflow (order state machine, timeline)
                     |-- Services (catalog, service requests)
                     |-- SLA Clocks (watchdog + status)
                     |-- Notifications (email/SMS/webhook)
                     |
                     |-- Event Store + Projections (read models)
```

Primary UI surfaces:

- Holmes.Internal (admin/ops SPA)
- Holmes.IntakeSessions (subject-facing intake SPA)

---

## 3) Runtime Hosts and Projects

**API + background services**

- `src/Holmes.App.Server`
  - API controllers, SSE endpoints, and background services.
  - Hosts `SlaClockWatchdogService`, `NotificationProcessingService`, and `DeferredDispatchProcessor`.

**Integration boundary**

- `src/Holmes.App.Integration`
  - Cross-module event handlers and gateways (Intake -> Workflow, Workflow -> Services, etc.).

**Cross-cutting infrastructure**

- `src/Holmes.App.Infrastructure`
  - Security and infrastructure utilities, intentionally database-agnostic.

**Identity**

- `src/Holmes.Identity.Server`
  - Duende IdentityServer + ASP.NET Identity for auth.

**SPAs**

- `src/Holmes.Internal` + `src/Holmes.Internal.Server`
- `src/Holmes.Intake` + `src/Holmes.Intake.Server`

---

## 4) Current Bounded Contexts

**Implemented modules under `src/Modules/`**

1. **Core**: shared primitives, unit of work, event store, domain event serialization.
2. **Subjects**: canonical identity and merge lineages.
3. **Users**: role assignments and user projection model.
4. **Customers**: customer registry, contacts, and projections.
5. **Intake**: intake sessions, consent capture, answers snapshots.
6. **Workflow**: order state machine, order summary/timeline projections.
7. **Services**: service catalog and service request lifecycle.
8. **SlaClocks**: SLA clocks, watchdog, and clock projections.
9. **Notifications**: notification requests, processing, and projection history.

---

## 5) Cross-Module Choreography (Phase 3 Focus)

These handlers connect the modules into an end-to-end workflow:

- `IntakeToWorkflowHandler` (`src/Holmes.App.Integration/EventHandlers/IntakeToWorkflowHandler.cs`)
  - `IntakeSessionInvited` -> `RecordOrderInviteCommand`
  - `IntakeSessionStarted` -> `MarkOrderIntakeStartedCommand`

- `OrderWorkflowGateway` (`src/Holmes.App.Integration/Gateways/OrderWorkflowGateway.cs`)
  - `SubmitIntakeCommand` -> `MarkOrderIntakeSubmittedCommand`
  - `AcceptIntakeSubmissionCommand` -> `MarkOrderReadyForFulfillmentCommand`
  - Policy validation is currently stubbed to allow all submissions.

- `OrderStatusChangedSlaHandler` (`src/Holmes.App.Integration/EventHandlers/OrderStatusChangedSlaHandler.cs`)
  - Starts/completes/pauses/resumes SLA clocks based on Order status changes.

- `OrderFulfillmentHandler` (`src/Holmes.App.Integration/EventHandlers/OrderFulfillmentHandler.cs`)
  - On `ReadyForFulfillment`, creates Service requests from the customer catalog and then moves the Order to
    `FulfillmentInProgress`.

- `ServiceCompletionOrderHandler` (`src/Holmes.App.Integration/EventHandlers/ServiceCompletionOrderHandler.cs`)
  - When all services complete for an Order, advances it to `ReadyForReport`.

- Notification triggers (`src/Holmes.App.Integration/EventHandlers/*.cs`)
  - Intake invite notifications are live; SLA at-risk/breached handlers log and are ready to wire to notifications.

---

## 6) Eventing, Unit of Work, and Deferred Dispatch

- All aggregates inherit `AggregateRoot` and emit domain events.
- `UnitOfWork<TContext>` persists aggregate state and `EventRecord` in one transaction.
- Domain events are dispatched through MediatR after commit.
- Optional deferred dispatch exists for an outbox-style flow (`DeferredDispatchProcessor`).

Key implementation references:

- `src/Modules/Core/Holmes.Core.Infrastructure.Sql/UnitOfWork.cs`
- `src/Modules/Core/Holmes.Core.Infrastructure.Sql/Entities/EventRecord.cs`

---

## 7) Projections (CQRS Read Models)

Read models are updated via event handlers in each module.

- Users: `UserProjectionHandler`
- Customers: `CustomerProjectionHandler`
- Subjects: `SubjectProjectionHandler`
- Intake: `IntakeSessionProjectionHandler`
- Workflow: `OrderSummaryProjectionRunner`, `OrderTimelineProjectionRunner`
- Services: `ServiceProjectionHandler`
- SLA Clocks: `SlaClockProjectionHandler`
- Notifications: `NotificationProjectionHandler`

Projection runners live in each module's Infrastructure project, and are invoked from the host.

---

## 8) Server-Sent Events (SSE)

Live updates are published through SSE endpoints in `Holmes.App.Server`:

- Orders: `GET /api/orders/changes`
- Services: `GET /api/services/changes`
- SLA clocks: `GET /api/clocks/sla/changes`

Each endpoint supports `Last-Event-ID` and order filters for resume semantics.

---

## 9) Module Conventions and Boundaries

Each module follows the standard layering pattern:

- `Holmes.<Feature>.Domain`
- `Holmes.<Feature>.Application.Abstractions`
- `Holmes.<Feature>.Application`
- `Holmes.<Feature>.Infrastructure.Sql`

Cross-module references are restricted to `*.Application.Abstractions` only.

---

## 10) Backlog / Not Implemented Yet

The following areas are planned but not present in this repository today:

- Compliance policy packs and regulatory clocks
- Adverse action workflow + evidence packs
- Adjudication engine + charge taxonomy
- Provider adapters beyond basic stubs
- Monetization and entitlements


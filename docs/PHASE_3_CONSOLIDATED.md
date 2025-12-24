# Phase 3.x Consolidated Status — SLA, Notifications, Services, Frontend

**Last Updated:** 2025-12-23 (code review pass)
**Status:** In Progress

This document tracks Phase 3.0, 3.1, and 3.2 and is based on the current codebase. Frontend status reflects code
presence, not a verified UI run in this pass.

---

## Executive Summary

| Phase   | Focus                      | Backend Status | Frontend Status | Overall |
|---------|----------------------------|----------------|-----------------|---------|
| **3.0** | SLA Clocks & Notifications | ✅ Implemented | 🟡 Not re-verified | 85%   |
| **3.1** | Services & Fulfillment     | ✅ Implemented | 🟡 Not re-verified | 85%   |
| **3.2** | Subject Data & Frontend    | 🟡 Partial     | 🟡 Scaffolded      | 40%   |

**Bottom line:** The Phase 3 integration path is working in code: Intake -> Workflow -> Services -> ReadyForReport,
with SLA clocks and notifications wired through background services and projections. Remaining work is primarily in
Subject data expansion and Intake UI completion.

---

## Phase 3.0 — SLA Clocks & Notifications

### Backend: ✅ IMPLEMENTED

**SlaClocks Module** (`src/Modules/SlaClocks/`)

- [x] `SlaClock` aggregate with state machine (Running -> AtRisk -> Breached -> Paused -> Completed)
- [x] `ClockKind` enum: Intake, Fulfillment, Overall, Custom
- [x] Domain events: Started, AtRisk, Breached, Paused, Resumed, Completed
- [x] Commands: Start, Pause, Resume, MarkAtRisk, MarkBreached, Complete
- [x] `BusinessCalendarService` with US Federal holidays (2024-2026) + customer overrides
- [x] `SlaClockWatchdogService` background service in `src/Holmes.App.Server`
- [x] `OrderStatusChangedSlaHandler` integration
- [x] Unit tests: `SlaClockTests`, `BusinessCalendarServiceTests`, `SlaClockProjectionHandlerTests`
- [x] Read-only projections: `sla_clock_projections` with `SlaClockProjectionHandler`

**Notifications Module** (`src/Modules/Notifications/`)

- [x] `Notification` aggregate with delivery lifecycle
- [x] Channels: Email, SMS, Webhook (logging/stub providers)
- [x] Commands: Create, Process, RecordDeliveryResult
- [x] `NotificationProcessingService` background service in `src/Holmes.App.Server`
- [x] Unit tests: `NotificationTests`, `NotificationProjectionHandlerTests`
- [x] Read-only projections: `notification_projections`

**Integration notes**

- Intake invite notifications are sent via `IntakeInviteNotificationHandler`.
- SLA at-risk/breached handlers exist but currently log only; they do not create notifications yet.

### Frontend: 🟡 NOT RE-VERIFIED

Known routes/components exist in Holmes.Internal, but UI behavior was not re-validated in this pass.

---

## Phase 3.1 — Services & Fulfillment

### Backend: ✅ IMPLEMENTED

**Services Module** (`src/Modules/Services/`)

- [x] Aggregate named `Service` with state machine (Pending -> Dispatched -> InProgress -> Completed/Failed/Canceled)
- [x] Service type taxonomy and customer `ServiceCatalog`
- [x] Tier configuration (customer-defined execution order)
- [x] Commands: Create, Dispatch, Cancel, Retry, RecordResult, ProcessVendorCallback
- [x] Queries: GetServicesByOrder, GetService, GetCustomerServiceCatalog, ListServiceTypes,
  GetOrderCompletionStatus
- [x] `IVendorAdapter` + `StubVendorAdapter`
- [x] `IServiceChangeBroadcaster` for SSE
- [x] Read-only projections: `service_projections` with `ServiceProjectionHandler`

**Integration handlers**

- `OrderFulfillmentHandler` creates service requests when Order reaches `ReadyForFulfillment` and then calls
  `BeginOrderFulfillmentCommand` if any services were created.
- `ServiceCompletionOrderHandler` advances Order to `ReadyForReport` once all services are complete.

### Frontend: 🟡 NOT RE-VERIFIED

Known components exist (FulfillmentDashboardPage, Services tab, ServiceCatalogEditor), but UI behavior was not
re-validated in this pass.

---

## Phase 3.2 — Subject Data Expansion & Frontend

### Backend: 🟡 PARTIAL

**Subject domain expansion needed:**

- [ ] `SubjectAddress` collection (with county FIPS)
- [ ] `SubjectEmployment` collection
- [ ] `SubjectEducation` collection
- [ ] `SubjectReference` collection
- [ ] `SubjectPhone` collection
- [ ] Encrypted SSN storage with last-4 accessor
- [ ] EF Core configurations and migrations
- [ ] Commands: AddSubjectAddress, AddSubjectEmployment, etc.
- [ ] Update `SubmitIntakeCommand` to persist all collections

**County resolution:**

- [ ] `ICountyResolutionService` interface
- [ ] ZIP-to-County lookup table (covers ~95%)

### Frontend: 🟡 SCAFFOLDED

**Holmes.IntakeSessions scaffolds:**

- [x] `AddressHistoryForm.tsx`
- [x] `EmploymentHistoryForm.tsx`
- [x] `EducationHistoryForm.tsx`
- [x] `ReferenceForm.tsx`
- [x] `IntakeFlow.tsx`

**Holmes.IntakeSessions gaps:**

- [ ] Dynamic form sections based on policy
- [ ] Wire forms to intake submission API
- [ ] Progress persistence to encrypted snapshot
- [ ] Review step showing all collected data
- [ ] Multi-address with date range validation
- [ ] Policy-driven field requirements (7-year address history, etc.)

**Holmes.Internal gaps:**

- [ ] Customer detail page with Services/Tiers tabs (components exist, not wired)
- [ ] Subject detail page showing address/employment/education history

---

## API Endpoint Status (from code)

| Endpoint                                  | Controller                | Status    |
|-------------------------------------------|---------------------------|-----------|
| `GET/POST /api/customers`                 | CustomersController       | ✅ Exists |
| `GET/POST /api/subjects`                  | SubjectsController        | ✅ Exists |
| `GET/POST /api/orders`                    | OrdersController          | ✅ Exists |
| `GET/POST /api/users`                     | UsersController           | ✅ Exists |
| `GET /api/intake/sessions`                | IntakeSessionsController  | ✅ Exists |
| `GET /api/services/{orderId}`             | ServicesController        | ✅ Exists |
| `SSE /api/orders/changes`                 | OrderChangesController    | ✅ Exists |
| `SSE /api/services/changes`               | ServiceChangesController  | ✅ Exists |
| `SSE /api/clocks/sla/changes`             | SlaClockChangesController | ✅ Exists |
| `GET /api/clocks/sla?orderId={id}`        | SlaClocksController       | ✅ Exists |
| `POST /api/clocks/sla/{id}/pause`         | SlaClocksController       | ✅ Exists |
| `POST /api/clocks/sla/{id}/resume`        | SlaClocksController       | ✅ Exists |
| `GET /api/notifications`                  | NotificationsController   | ✅ Exists |
| `GET /api/notifications/{id}`             | NotificationsController   | ✅ Exists |
| `POST /api/notifications/{id}/retry`      | NotificationsController   | ✅ Exists |
| `GET /api/services/queue`                 | ServicesController        | ✅ Exists |
| `POST /api/services/{id}/retry`           | ServicesController        | ✅ Exists |
| `POST /api/services/{id}/cancel`          | ServicesController        | ✅ Exists |
| `GET /api/customers/{id}/service-catalog` | CustomersController       | ✅ Exists |
| `PUT /api/customers/{id}/service-catalog` | CustomersController       | ✅ Exists |

---

## Immediate Priorities (Phase 3 Focus)

1. Finish Intake UI and persist full Subject data collections.
2. Implement Subject data expansion + migrations.
3. Wire SLA at-risk/breached notifications to real Notifications.
4. Validate Holmes.Internal UI integration (services, clocks, notifications) after recent changes.

---

## Related Documents

- `docs/ARCHITECTURE.md` — system architecture
- `docs/STATE_MACHINES.md` — lifecycle details
- `docs/PLAN.md` — overall roadmap
- `docs/domain/INTAKE_SESSION.md` — intake domain model
- `docs/domain/INTAKE_UI.md` — intake UI specification
- `docs/Holmes.App.UI.md` — UI architecture guidelines

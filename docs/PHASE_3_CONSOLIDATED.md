# Phase 3.x Consolidated Status — SLA, Notifications, Services, Frontend

**Last Updated:** 2025-12-14 (Frontend verified working)
**Status:** In Progress

This document consolidates Phase 3, 3.1, and 3.2 into a single tracking document. It replaces the individual
phase documents and the monetize folder overlays for delivery tracking purposes.

---

## Executive Summary

| Phase   | Focus                      | Backend Status | Frontend Status | Overall |
|---------|----------------------------|----------------|-----------------|---------|
| **3.0** | SLA Clocks & Notifications | ✅ Complete     | ✅ Verified      | 95%     |
| **3.1** | Services & Fulfillment     | ✅ Complete     | ✅ Verified      | 95%     |
| **3.2** | Subject Data & Frontend    | 🟡 Partial     | 🟡 Scaffolded   | 40%     |

**Bottom line:** Backend aggregates, commands, read-only projections, and API endpoints exist for all modules.
The order fulfillment handler is complete — `OrderFulfillmentHandler` creates ServiceRequests when Order reaches
`ReadyForFulfillment` and transitions to `FulfillmentInProgress`. `ServiceCompletionOrderHandler` advances
Order to `ReadyForReport` when all services complete. All API endpoints for SLA Clocks, Notifications, Services
queue, and Customer service catalog are now implemented. **Frontend verified working** — OrderDetailPage includes
SLA Clocks tab (with pause/resume for Running clocks), Notifications tab, Services tab with tier progress, Audit
Log tab, and Timeline tab. Customer Service Catalog editor works (partial: tier auto-creation needed when
assigning services to new tiers). SeedData.cs creates demo orders with SLA clocks and service requests.

---

## Phase 3.0 — SLA Clocks & Notifications

### Backend: ✅ COMPLETE

**SlaClocks Module** (`src/Modules/SlaClocks/`)

- [x] `SlaClock` aggregate with state machine (Running → AtRisk → Breached → Paused → Completed)
- [x] `ClockKind` enum: Intake, Fulfillment, Overall, Service, Custom
- [x] Domain events: Started, AtRisk, Breached, Paused, Resumed, Completed
- [x] Commands: Start, Pause, Resume, MarkAtRisk, MarkBreached, Complete
- [x] `BusinessCalendarService` with US Federal holidays (2024-2026)
- [x] `SlaClockWatchdogService` background service
- [x] `OrderStatusChangedSlaHandler` integration
- [x] Unit tests: `SlaClockTests`, `BusinessCalendarServiceTests`, `SlaClockProjectionHandlerTests`
- [x] **Read-only projections** (`sla_clock_projections` table, `SlaClockProjectionHandler`,
  `SlaClockEventProjectionRunner`)

**Notifications Module** (`src/Modules/Notifications/`)

- [x] `NotificationRequest` aggregate with delivery lifecycle
- [x] Channels: Email, SMS, Webhook
- [x] Triggers: IntakeSessionInvited, SlaClockAtRisk, SlaClockBreached, etc.
- [x] Commands: Create, Process, RecordDeliveryResult
- [x] Stub providers: LoggingEmailProvider, LoggingSmsProvider, LoggingWebhookProvider
- [x] `NotificationProcessingService` background service
- [x] Unit tests: `NotificationRequestTests`, `NotificationProjectionHandlerTests`
- [x] **Read-only projections** (`notification_projections` table, `NotificationProjectionHandler`,
  `NotificationEventProjectionRunner`)

### Frontend: 🟡 APIs READY

**API endpoints complete:**

- [x] `GET /api/clocks/sla?orderId={id}` — returns clock data for order
- [x] `POST /api/clocks/sla/{id}/pause` — pause a clock
- [x] `POST /api/clocks/sla/{id}/resume` — resume a clock
- [x] `GET /api/notifications?orderId={id}` — returns notifications for order
- [x] `GET /api/notifications/{id}` — get single notification
- [x] `POST /api/notifications/{id}/retry` — retry failed notification

**Holmes.Internal implemented (VERIFIED):**

- [x] SLA Clocks tab on Order detail page (shows all clocks with status, pause/resume for Running clocks)
- [x] Notifications tab on Order detail page (shows notification history with retry action)
- [x] `SlaBadge` component wired to real API data via `clockStateToSlaStatus` helper
- [x] React Query hooks: `useOrderSlaClocks`, `usePauseSlaClock`, `useResumeSlaClock`
- [x] React Query hooks: `useOrderNotifications`, `useRetryNotification`
- [x] TypeScript types for SLA clocks and notifications in `@/types/api`
- [x] Audit Log tab with proper event name formatting (assembly-qualified names handled)
- [x] Timeline tab showing order activity events

**Holmes.Internal gaps (remaining):**

- [ ] SLA clock dashboard (show at-risk/breached counts aggregated across orders)
- [ ] SSE extension for `clock.at_risk`, `clock.breached` events

---

## Phase 3.1 — Services & Fulfillment

### Backend: ✅ COMPLETE

**Services Module** (`src/Modules/Services/`)

- [x] `ServiceRequest` aggregate with state machine (Pending → Dispatched → InProgress → Completed/Failed/Canceled)
- [x] Service type taxonomy (Criminal, Identity, Employment, Education, Driving, Credit, Drug, Civil, Reference,
  Healthcare, Custom)
- [x] `ServiceCatalog` for customer-specific service configuration
- [x] Tier execution logic (customer-defined execution order)
- [x] Commands: Create, Dispatch, Cancel, Retry, RecordResult, ProcessVendorCallback
- [x] Commands: UpdateCatalogService, UpdateTierConfiguration
- [x] Queries: GetServiceRequestsByOrder, GetServiceRequest, GetCustomerServiceCatalog, ListServiceTypes,
  GetOrderCompletionStatus
- [x] `IVendorAdapter` interface with credential store abstraction
- [x] `IServiceChangeBroadcaster` for SSE
- [x] **Read-only projections** (`service_projections` table, `ServiceProjectionHandler`,
  `ServiceEventProjectionRunner`)
- [x] Unit tests: `ServiceRequestTests`, `ServiceProjectionHandlerTests`
- [x] **Order fulfillment handler** — `OrderFulfillmentHandler` creates ServiceRequests when Order reaches
  `ReadyForFulfillment`
- [x] **Service completion handler** — `ServiceCompletionOrderHandler` advances Order to `ReadyForReport` when all
  services complete

**Controllers:**

- [x] `ServicesController` — CRUD for service requests
- [x] `ServiceChangesController` — SSE for service status updates

### Frontend: 🟡 APIs READY

**API endpoints complete:**

- [x] `GET /api/services/queue` — fulfillment queue with customer access filtering
- [x] `GET /api/services/{orderId}` — services for an order
- [x] `GET /api/customers/{id}/service-catalog` — customer service catalog
- [x] `PUT /api/customers/{id}/service-catalog` — update customer catalog

**Holmes.Internal implemented:**

- [x] `FulfillmentDashboardPage` — stats cards, filters, data grid (wired to real API)
- [x] `ServiceStatusCard` component
- [x] `TierProgressView` component
- [x] `ServiceCatalogEditor` component (wired to customer catalog APIs)
- [x] `TierConfigurationEditor` component (wired to customer catalog APIs)
- [x] Types defined in `@/types/api` for ServiceCategory, ServiceStatus, etc.
- [x] Services tab on `OrderDetailPage` with real data

**Gaps (remaining):**

- [ ] SSE integration for real-time service updates
- [ ] Retry/Cancel actions calling real endpoints

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

**Holmes.Intake has scaffolds:**

- [x] `AddressHistoryForm.tsx` — exists but needs completion
- [x] `EmploymentHistoryForm.tsx` — exists but needs completion
- [x] `EducationHistoryForm.tsx` — exists but needs completion
- [x] `ReferenceForm.tsx` — exists but needs completion
- [x] `IntakeFlow.tsx` — exists but incomplete

**Holmes.Intake gaps:**

- [ ] Dynamic form sections based on policy
- [ ] Wire forms to intake submission API
- [ ] Progress persistence to encrypted snapshot
- [ ] Review step showing all collected data
- [ ] Multi-address with date range validation
- [ ] Policy-driven field requirements (7-year address history, etc.)

**Holmes.Internal gaps:**

- [ ] Customer detail page with Services/Tiers tabs (components exist, not wired)
- [ ] Subject detail page showing address/employment/education history
- [ ] Real API integration for customer service catalog

---

## API Endpoint Status

### Implemented (Controllers exist)

| Endpoint                                  | Controller               | Status    |
|-------------------------------------------|--------------------------|-----------|
| `GET/POST /api/customers`                 | CustomersController      | ✅ Working |
| `GET/POST /api/subjects`                  | SubjectsController       | ✅ Working |
| `GET/POST /api/orders`                    | OrdersController         | ✅ Working |
| `GET/POST /api/users`                     | UsersController          | ✅ Working |
| `GET /api/intake/sessions`                | IntakeSessionsController | ✅ Working |
| `GET /api/services/{orderId}`             | ServicesController       | ✅ Exists  |
| `SSE /api/order-changes`                  | OrderChangesController   | ✅ Working |
| `SSE /api/service-changes`                | ServiceChangesController | ✅ Exists  |
| `GET /api/clocks/sla?orderId={id}`        | SlaClocksController      | ✅ Working |
| `POST /api/clocks/sla/{id}/pause`         | SlaClocksController      | ✅ Working |
| `POST /api/clocks/sla/{id}/resume`        | SlaClocksController      | ✅ Working |
| `GET /api/notifications`                  | NotificationsController  | ✅ Working |
| `GET /api/notifications/{id}`             | NotificationsController  | ✅ Working |
| `POST /api/notifications/{id}/retry`      | NotificationsController  | ✅ Working |
| `GET /api/services/queue`                 | ServicesController       | ✅ Working |
| `GET /api/customers/{id}/service-catalog` | CustomersController      | ✅ Working |
| `PUT /api/customers/{id}/service-catalog` | CustomersController      | ✅ Working |

### Needed but Missing

| Endpoint                             | Purpose            | Priority |
|--------------------------------------|--------------------|----------|
| `GET /api/subjects/{id}/addresses`   | Subject addresses  | Medium   |
| `GET /api/subjects/{id}/employments` | Subject employment | Medium   |

---

## Documentation Cleanup

### Files to keep (authoritative):

- `docs/ARCHITECTURE.md` — main architecture reference
- `docs/PLAN.md` — high-level phase roadmap
- `docs/PHASE_3_CONSOLIDATED.md` — THIS FILE (replaces individual phase docs for tracking)
- `docs/DEV_SETUP.md` — developer setup guide
- `docs/RUNBOOKS.md` — operational runbooks
- `docs/MODULE_TEMPLATE.md` — module scaffolding guide

### Files to archive/merge:

- `docs/PHASE_3.md` → content merged here
- `docs/PHASE_3_1.md` → content merged here
- `docs/PHASE_3_2.md` → content merged here
- `docs/SLA_CLOCKS_PLAN.md` → implementation complete, archive
- `docs/monetize/PLAN_PHASE_3_UPDATE.md` → merge into PLAN.md
- `docs/monetize/ARCHITECTURE_COMPLIANCE_UPDATE.md` → merge into ARCHITECTURE.md

---

## Immediate Priorities

### Priority 1: Backend Projections & Routing ✅ COMPLETE

Read-only projections are needed before frontend can wire to real APIs.

1. ~~**Add SlaClocks read-only projections**~~ ✅ DONE (`sla_clock_projections` table, `SlaClockProjectionHandler`,
   `SlaClockEventProjectionRunner`)
2. ~~**Add Notifications read-only projections**~~ ✅ DONE (`notification_projections` table,
   `NotificationProjectionHandler`, `NotificationEventProjectionRunner`)
3. ~~**Add Services read-only projections**~~ ✅ DONE (`service_projections` table, `ServiceProjectionHandler`,
   `ServiceEventProjectionRunner`)
4. ~~**Create order fulfillment handler**~~ ✅ DONE — `OrderFulfillmentHandler` creates ServiceRequests when Order
   reaches `ReadyForFulfillment`, then transitions to `FulfillmentInProgress`. `ServiceCompletionOrderHandler`
   transitions Order to `ReadyForReport` when all services complete.

### Priority 2: Wire Frontend to Real APIs

Once projections exist, wire frontend components.

1. ~~**Create SLA Clocks controller**~~ ✅ DONE — `SlaClocksController` with `GET /api/clocks/sla?orderId={id}`,
   `POST /api/clocks/sla/{id}/pause`, `POST /api/clocks/sla/{id}/resume`
2. ~~**Create Notifications controller**~~ ✅ DONE — `NotificationsController` with `GET /api/notifications`,
   `GET /api/notifications/{id}`, `POST /api/notifications/{id}/retry`
3. ~~**Create Services queue endpoint**~~ ✅ DONE — `GET /api/services/queue` for fulfillment queue
4. ~~**Create Customer catalog endpoints**~~ ✅ DONE — `GET/PUT /api/customers/{id}/service-catalog`
5. ~~**Wire FulfillmentDashboardPage**~~ ✅ DONE — `useFulfillmentQueue` hook with server-side pagination
6. ~~**Add Services tab to OrderDetailPage**~~ ✅ DONE — `useOrderServices` hook with `TierProgressView`
7. ~~**Wire ServiceCatalogEditor**~~ ✅ DONE — `useCustomerCatalog` and `useUpdateServiceCatalog` hooks

### Priority 3: Complete Intake Flow

1. **Finish AddressHistoryForm** with date range validation
2. **Finish EmploymentHistoryForm** with all fields
3. **Wire IntakeFlow** to submission API
4. **Add policy-driven section visibility**

### Priority 4: Subject Domain Expansion

1. **Add address collection** to Subject aggregate
2. **Create EF migrations** for subject_addresses, etc.
3. **Update SubmitIntakeCommand** to persist collections
4. **Add Subject API endpoints** for address/employment data

---

## Acceptance Criteria (Phase 3.x Exit)

### Must Have

- [x] SLA clocks visible in UI with at-risk/breached indicators
- [x] Notifications history viewable per order
- [x] Fulfillment dashboard showing real service data
- [x] Order detail shows service status with real data
- [ ] Intake form captures address history (7 years)
- [x] Customer service catalog configurable via UI (partial: tier auto-creation needed)

### Should Have

- [ ] SSE real-time updates for service status
- [ ] Retry/Cancel service actions work
- [ ] Employment/Education captured in intake
- [x] Clock pause/resume from UI (works for Running clocks; hidden for terminal states)

### Deferred (Post Phase 3.x)

- [ ] Grafana dashboards
- [ ] Alerting configuration
- [ ] Full intake flow with all sections (education, references)

---

## Related Documents

- `docs/ARCHITECTURE.md` — system architecture
- `docs/PLAN.md` — overall roadmap
- `docs/domain/INTAKE_SESSION.md` — intake domain model
- `docs/domain/INTAKE_UI.md` — intake UI specification
- `docs/Holmes.App.UI.md` — UI architecture guidelines

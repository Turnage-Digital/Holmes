# Phase 3.x Consolidated Status — SLA, Notifications, Services, Frontend

**Last Updated:** 2025-12-12 (SLA Clock projections added)
**Status:** In Progress

This document consolidates Phase 3, 3.1, and 3.2 into a single tracking document. It replaces the individual
phase documents and the monetize folder overlays for delivery tracking purposes.

---

## Executive Summary

| Phase | Focus | Backend Status | Frontend Status | Overall |
|-------|-------|----------------|-----------------|---------|
| **3.0** | SLA Clocks & Notifications | ✅ SLA complete, 🟡 Notifications | ❌ Not integrated | 70% |
| **3.1** | Services & Fulfillment | 🟡 Missing projections + routing trigger | 🟡 Mock data | 55% |
| **3.2** | Subject Data & Frontend | 🟡 Partial | 🟡 Scaffolded | 40% |

**Bottom line:** Backend aggregates and commands exist, but read-only projections are missing and
nothing triggers service creation when orders are ready for routing. Frontend is scaffolded but uses
mock data and hasn't been wired to real APIs.

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
- [x] **Read-only projections** (`sla_clock_projections` table, `SlaClockProjectionHandler`, `SlaClockEventProjectionRunner`)

**Notifications Module** (`src/Modules/Notifications/`)
- [x] `NotificationRequest` aggregate with delivery lifecycle
- [x] Channels: Email, SMS, Webhook
- [x] Triggers: IntakeSessionInvited, SlaClockAtRisk, SlaClockBreached, etc.
- [x] Commands: Create, Process, RecordDeliveryResult
- [x] Stub providers: LoggingEmailProvider, LoggingSmsProvider, LoggingWebhookProvider
- [x] `NotificationProcessingService` background service
- [x] Unit tests: `NotificationRequestTests`
- [ ] **Read-only projections** (`notification_history` read model for queries)

### Frontend: ❌ NOT INTEGRATED

**Holmes.Internal gaps:**
- [ ] SLA clock dashboard (show at-risk/breached counts)
- [ ] Notification history view
- [ ] Clock detail panel on Order detail page
- [ ] Wire `SlaBadge` component to real API data

**API endpoints needed:**
- [ ] `GET /api/clocks/sla?orderId={id}` — returns clock data for order
- [ ] `GET /api/notifications?orderId={id}` — returns notifications for order
- [ ] SSE extension for `clock.at_risk`, `clock.breached` events

### Observability: DEFERRED

Grafana dashboards and alerting are deferred to post-Phase 3.x. Basic logging exists.

---

## Phase 3.1 — Services & Fulfillment

### Backend: 🟡 MOSTLY COMPLETE

**Services Module** (`src/Modules/Services/`)
- [x] `ServiceRequest` aggregate with state machine (Pending → Dispatched → InProgress → Completed/Failed/Canceled)
- [x] Service type taxonomy (Criminal, Identity, Employment, Education, Driving, Credit, Drug, Civil, Reference, Healthcare, Custom)
- [x] `ServiceCatalog` for customer-specific service configuration
- [x] Tier execution logic (customer-defined execution order)
- [x] Commands: Create, Dispatch, Cancel, Retry, RecordResult, ProcessVendorCallback
- [x] Commands: UpdateCatalogService, UpdateTierConfiguration
- [x] Queries: GetServiceRequestsByOrder, GetServiceRequest, GetCustomerServiceCatalog, ListServiceTypes, GetOrderCompletionStatus
- [x] `IVendorAdapter` interface with credential store abstraction
- [x] `IServiceChangeBroadcaster` for SSE
- [ ] **Read-only projections** (`service_requests` read model for queries)
- [ ] **Order routing trigger** — handler to create ServiceRequests when Order reaches `ReadyForFulfillment`

**Controllers:**
- [x] `ServicesController` — CRUD for service requests
- [x] `ServiceChangesController` — SSE for service status updates

### Frontend: 🟡 MOCK DATA

**Holmes.Internal implemented (with mock data):**
- [x] `FulfillmentDashboardPage` — stats cards, filters, data grid (MOCK DATA)
- [x] `ServiceStatusCard` component
- [x] `TierProgressView` component
- [x] `ServiceCatalogEditor` component
- [x] `TierConfigurationEditor` component
- [x] Types defined in `@/types/api` for ServiceCategory, ServiceStatus, etc.

**Gaps:**
- [ ] Wire `FulfillmentDashboardPage` to real `/api/services/queue` endpoint
- [ ] Wire service components to real APIs
- [ ] Add Services tab to `OrderDetailPage` with real data
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

| Endpoint | Controller | Status |
|----------|------------|--------|
| `GET/POST /api/customers` | CustomersController | ✅ Working |
| `GET/POST /api/subjects` | SubjectsController | ✅ Working |
| `GET/POST /api/orders` | OrdersController | ✅ Working |
| `GET/POST /api/users` | UsersController | ✅ Working |
| `GET /api/intake/sessions` | IntakeSessionsController | ✅ Working |
| `GET /api/services/{orderId}` | ServicesController | ✅ Exists |
| `SSE /api/order-changes` | OrderChangesController | ✅ Working |
| `SSE /api/service-changes` | ServiceChangesController | ✅ Exists |

### Needed but Missing

| Endpoint | Purpose | Priority |
|----------|---------|----------|
| `GET /api/clocks/sla` | Query SLA clocks | High |
| `POST /api/clocks/sla/{id}/pause` | Pause clock | Medium |
| `POST /api/clocks/sla/{id}/resume` | Resume clock | Medium |
| `GET /api/notifications` | Query notifications | High |
| `POST /api/notifications/{id}/retry` | Retry notification | Medium |
| `GET /api/services/queue` | Fulfillment queue | High |
| `GET /api/customers/{id}/service-catalog` | Customer services | High |
| `PUT /api/customers/{id}/service-catalog` | Update services | Medium |
| `GET /api/subjects/{id}/addresses` | Subject addresses | Medium |
| `GET /api/subjects/{id}/employments` | Subject employment | Medium |

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

### Priority 1: Backend Projections & Routing

Read-only projections are needed before frontend can wire to real APIs.

1. ~~**Add SlaClocks read-only projections**~~ ✅ DONE (`sla_clock_projections` table, `SlaClockProjectionHandler`, `SlaClockEventProjectionRunner`)
2. **Add Notifications read-only projections** (`notification_history` table, projection handler)
3. **Add Services read-only projections** (`service_requests` table, projection handler)
4. **Create order routing handler** — when Order reaches `ReadyForFulfillment`, create ServiceRequests based on customer catalog

### Priority 2: Wire Frontend to Real APIs

Once projections exist, wire frontend components.

1. **Create SLA Clocks controller** with query endpoints
2. **Wire FulfillmentDashboardPage** to real service queue endpoint
3. **Add Services tab to OrderDetailPage** with real service data
4. **Wire ServiceCatalogEditor** to customer catalog APIs

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

- [ ] SLA clocks visible in UI with at-risk/breached indicators
- [ ] Notifications history viewable per order
- [ ] Fulfillment dashboard showing real service data
- [ ] Order detail shows service status with real data
- [ ] Intake form captures address history (7 years)
- [ ] Customer service catalog configurable via UI

### Should Have

- [ ] SSE real-time updates for service status
- [ ] Retry/Cancel service actions work
- [ ] Employment/Education captured in intake
- [ ] Clock pause/resume from UI

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

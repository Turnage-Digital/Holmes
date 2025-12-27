# Phase 3 Delivery Plan — SLA Clocks & Notifications Foundation

**Phase Window:** Weeks 7–8
**Outcome Target:** Orders tracked by SLA clocks with at-risk/breach detection; notifications flow through
tenant-configured providers with delivery tracking.

**Commercial Alignment:** This phase delivers **SLA Visibility** and **Operational Notifications** — foundational
infrastructure that Phase 3.1 (Services) and Phase 3.2 (Compliance) build upon.

> **Note:** Phase 3 was split to address the Services architecture gap:
> - **Phase 3:** SLA Clocks & Notifications (this document) — MOSTLY COMPLETE
> - **Phase 3.1:** Services & Fulfillment (`PHASE_3_1.md`) — background check service orchestration
> - **Phase 3.2:** Compliance & Policy Gates — PP grants, disclosure acceptance, fair-chance overlays

## 1. Stakeholders & Working Cadence

- **Domain Steward (Eric Evans):** Facilitates event-storming for SLA/Compliance bounded contexts, validates clock
  semantics and policy overlay rules.
- **Product & Compliance Lead:** Owns regulatory requirements (FCRA timing, permissible purpose certification,
  disclosure
  acceptance flows), signs off on clock behavior and evidence capture.
- **Tech Leads (Backend & Infrastructure):** Own aggregate implementation, calendar service, notification abstractions,
  and Identity Broker federation.
- **Ops & Observability Partner:** Validates clock dashboards, alert thresholds, and notification delivery monitoring.

Standing ceremonies:

1. **Event Storm (2 × 2 hrs):** Map SLA clock lifecycle, compliance policy application, and notification trigger points.
2. **Compliance Review (weekly):** Validate PP grant flows, disclosure acceptance evidence, and clock enforcement rules
   with counsel.
3. **Build Checkpoint (twice weekly):** Track aggregate progress, calendar service readiness, notification provider
   integration, and Identity Broker milestones.
4. **Runbook Review (weekly):** Dry-run clock replay, notification retry scenarios, and IdP federation flows.

## 2. Scope Breakdown

| Track                         | Deliverables                                                                                                                                        | Definition of Done                                                                                                                                   |
|-------------------------------|-----------------------------------------------------------------------------------------------------------------------------------------------------|------------------------------------------------------------------------------------------------------------------------------------------------------|
| **SLA Clocks**                | `SlaClock` aggregate, business calendar service, holiday/jurisdiction models, clock state machine (running → at_risk → breached → paused → resumed) | Clocks compute deadlines correctly with business-day math; at-risk/breached transitions fire domain events; projection exposes queryable clock index |
| **Notifications**             | `Notification` aggregate, provider abstraction (`INotificationProvider`), tenant-configured routing (email/SMS/webhook), delivery tracking          | Holmes emits `NotificationCreated` events; providers deliver asynchronously; delivery proofs captured; retry/backoff implemented                     |
| **Read Models & Projections** | `sla_clocks`, `notification_history` projections with checkpointing                                                                                 | Replayable via projection runner; verification queries documented in runbooks                                                                        |
| **Observability & Ops**       | Clock health dashboards, notification delivery metrics; runbooks for clock replay and notification retry                                            | Grafana dashboards live; alerts for breached clocks and failed notifications; runbooks updated                                                       |

**Deferred to Phase 3.2:**

- Compliance Policy (PP grants, disclosure acceptance, fair-chance overlays)
- Compliance grants read model
- Adverse action clocks

## 3. Detailed Workstreams

### 3.1 SLA Clocks Module

#### 3.1.1 Domain Model

**Bounded Context:** `Holmes.SlaClocks`

**Aggregates:**

- `SlaClock` — tracks a single SLA obligation with deadline computation and state transitions

**Value Objects:**

- `ClockKind` — enum: `Intake`, `Fulfillment`, `Overall`, `Custom`
- `ClockState` — enum: `Running`, `AtRisk`, `Breached`, `Paused`, `Completed`
- `Deadline` — computed deadline timestamp with jurisdiction context

**Terminology Note:** The Order state machine uses `ReadyForRouting`/`RoutingInProgress` states internally,
but SLA tracking uses **Fulfillment** to describe what's happening: executing background check services
(court searches, employment verifications, education checks, etc.). "Fulfillment" better represents the
service execution phase rather than reducing it to mere "routing."

**Domain Events:**

- `SlaClockStarted` — clock begins tracking
- `SlaClockAtRisk` — threshold crossed (e.g., 80% of time elapsed)
- `SlaClockBreached` — deadline exceeded
- `SlaClockPaused` — external condition (dispute, hold) suspends countdown
- `SlaClockResumed` — countdown resumes
- `SlaClockCompleted` — target state reached before deadline

**Commands:**

- `StartSlaClockCommand` — initiates clock for an order
- `PauseSlaClockCommand` — suspends clock (with reason)
- `ResumeSlaClockCommand` — resumes paused clock
- `CompleteSlaClockCommand` — marks clock as satisfied

#### 3.1.2 Business Calendar Service

**Interface:** `IBusinessCalendarService`

```csharp
public interface IBusinessCalendarService
{
    DateTime AddBusinessDays(DateTime start, int days, IEnumerable<string> jurisdictions);
    DateTime AddBusinessHours(DateTime start, int hours, IEnumerable<string> jurisdictions);
    int GetBusinessDaysDiff(DateTime start, DateTime end, IEnumerable<string> jurisdictions);
    bool IsBusinessDay(DateTime date, IEnumerable<string> jurisdictions);
}
```

**EF Models:**

- `BusinessCalendar` — tenant + jurisdiction calendar definition
- `Holiday` — date + jurisdiction + observed flag
- `BusinessHours` — day-of-week operating hours per calendar

**Implementation Notes:**

- Calendars are tenant-scoped with optional jurisdiction overlays
- Federal holidays seeded by default; tenant can add custom holidays
- Business hours default to 9am–5pm local; configurable per tenant

#### 3.1.3 Clock Watchdog

A background service (`SlaClockWatchdogService`) periodically evaluates running clocks:

- Queries clocks approaching at-risk threshold
- Emits `SlaClockAtRisk` events when threshold crossed
- Emits `SlaClockBreached` events when deadline exceeded
- Configurable check interval (default: 1 minute)

#### 3.1.4 Read Model

**Table:** `sla.sla_clocks`

| Column             | Type         | Description                     |
|--------------------|--------------|---------------------------------|
| clock_id           | CHAR(26)     | ULID primary key                |
| order_id           | CHAR(26)     | Associated order                |
| kind               | VARCHAR(32)  | Clock type                      |
| state              | VARCHAR(32)  | Current state                   |
| started_at         | DATETIME(6)  | Clock start                     |
| deadline_at        | DATETIME(6)  | Computed deadline               |
| at_risk_at         | DATETIME(6)  | When at-risk threshold crossed  |
| breached_at        | DATETIME(6)  | When breach occurred            |
| paused_at          | DATETIME(6)  | Most recent pause               |
| completed_at       | DATETIME(6)  | When target reached             |
| pause_reason       | VARCHAR(128) | Why paused                      |
| jurisdictions      | JSON         | Jurisdiction codes for calendar |
| policy_snapshot_id | VARCHAR(64)  | Policy that defined SLA         |

### 3.2 Notifications Module

#### 3.2.1 Design Philosophy

**Holmes Does NOT Send Adverse Action Letters.** That's the critical liability boundary.

Holmes **does** send operational notifications:

- Intake invitations
- SLA alerts (at-risk, breached)
- Status updates
- Delivery confirmations

Holmes **does not** send:

- Adverse action letters (pre-adverse, final adverse)
- Dispute resolution correspondence
- Any communication with legal liability implications

For adverse action, Holmes emits a `NotificationCreated` event with `IsAdverseAction = true`. The tenant's
systems must handle the actual letter generation and delivery — they bear the legal responsibility.

**Tenant/Policy-Defined Rules:** Notification rules are configured at the tenant level (via customer policy),
not user self-service. Tenants define which events trigger notifications, to whom, via which channels.

**Immediate Delivery for Compliance-Critical:** Phase 3 focuses on immediate delivery. Batched/Daily digest
scheduling may be added in future phases for non-critical operational alerts.

#### 3.2.2 Domain Model (Implemented)

**Bounded Context:** `Holmes.Notifications`

**Location:** `src/Modules/Notifications/`

**Aggregates:**

- `Notification` — tracks a single notification through its lifecycle

**Enums:**

- `NotificationChannel` — `Email`, `Sms`, `Webhook` (no InApp)
- `NotificationTriggerType` — `IntakeSessionInvited`, `IntakeSubmissionReceived`, `ConsentCaptured`,
  `OrderStateChanged`, `SlaClockAtRisk`, `SlaClockBreached`, `NotificationFailed`
- `DeliveryStatus` — `Pending`, `Queued`, `Sending`, `Delivered`, `Failed`, `Bounced`, `Cancelled`
- `NotificationPriority` — `Low`, `Normal`, `High`, `Critical`

**Value Objects:**

- `NotificationRecipient` — channel, address, display name, metadata
- `NotificationContent` — subject, body, template ID, template data
- `NotificationTrigger` — trigger type, order/subject/customer IDs, from/to state, context
- `DeliveryAttempt` — channel, status, timestamps, attempt number, provider message ID, failure reason

**Domain Events:**

- `NotificationCreated` — notification created
- `NotificationQueued` — handed to provider
- `NotificationDelivered` — delivery confirmed
- `NotificationDeliveryFailed` — delivery failed (with reason and attempt number)
- `NotificationBounced` — permanent delivery failure
- `NotificationCancelled` — notification cancelled

**Commands:**

- `CreateNotificationCommand` — create notification request
- `ProcessNotificationCommand` — send via provider, record result
- `RecordDeliveryResultCommand` — update status from external callback

**Queries:**

- `GetNotificationsByOrderQuery` — list notifications for an order

#### 3.2.3 Provider Abstraction (Implemented)

**Interface:** `INotificationProvider`

```csharp
public interface INotificationProvider
{
    NotificationChannel Channel { get; }
    Task<NotificationSendResult> SendAsync(
        NotificationRecipient recipient,
        NotificationContent content,
        CancellationToken cancellationToken = default);
    bool CanHandle(NotificationChannel channel);
}
```

**Phase 3 Stub Implementations:**

- `LoggingEmailProvider` — logs to `Debug` instead of sending (swap for SendGrid later)
- `LoggingSmsProvider` — logs to `Debug` instead of sending (swap for Twilio later)
- `LoggingWebhookProvider` — logs to `Debug` instead of POSTing

All stubs are in `Infrastructure.Sql/Providers/` and return success with fake message IDs. They log the
full payload at `Debug` level so you can see exactly what would be sent during testing.

**Future Implementations:**

- `SendGridEmailProvider`
- `TwilioSmsProvider`
- `HttpWebhookProvider`

#### 3.2.4 The Adverse Action Boundary

When `ProcessNotificationCommand` encounters a notification with `IsAdverseAction = true`:

1. It marks the notification as `Queued` (acknowledging receipt)
2. It does NOT call any provider
3. It logs that the tenant must handle adverse action delivery
4. The notification stays in `Queued` status — the tenant's integration must call back to confirm delivery

This ensures Holmes never sends legally sensitive communications directly.

#### 3.2.5 Notification Triggers

Phase 3 notification triggers (tenant/policy-defined):

| Trigger Event              | Default Channels | Recipients                | Priority |
|----------------------------|------------------|---------------------------|----------|
| `IntakeSessionInvited`     | Email, SMS       | Subject                   | High     |
| `IntakeSubmissionReceived` | Webhook          | Customer                  | Normal   |
| `ConsentCaptured`          | Webhook          | Customer                  | Normal   |
| `OrderStateChanged`        | Webhook          | Customer                  | Normal   |
| `SlaClockAtRisk`           | Email, Webhook   | Ops, Customer             | High     |
| `SlaClockBreached`         | Email, Webhook   | Ops, Customer, Compliance | Critical |
| `NotificationFailed`       | Email            | Ops                       | High     |

#### 3.2.6 Database Schema

**Table:** `notifications.notification_requests`

| Column                     | Type         | Description                  |
|----------------------------|--------------|------------------------------|
| id                         | CHAR(26)     | ULID primary key             |
| customer_id                | CHAR(26)     | Tenant                       |
| order_id                   | CHAR(26)     | Associated order (nullable)  |
| subject_id                 | CHAR(26)     | Recipient subject (nullable) |
| trigger_type               | INT          | Trigger type enum            |
| channel                    | INT          | Channel enum                 |
| recipient_address          | VARCHAR(512) | Email/phone/URL              |
| recipient_display_name     | VARCHAR(256) | Display name                 |
| recipient_metadata_json    | JSON         | Additional recipient data    |
| content_subject            | VARCHAR(512) | Notification subject         |
| content_body               | TEXT         | Notification body            |
| content_template_id        | VARCHAR(128) | Template ID                  |
| content_template_data_json | JSON         | Template variables           |
| priority                   | INT          | Priority enum                |
| status                     | INT          | Delivery status enum         |
| is_adverse_action          | BOOLEAN      | Adverse action flag          |
| created_at                 | DATETIME(6)  | When requested               |
| processed_at               | DATETIME(6)  | When queued to provider      |
| delivered_at               | DATETIME(6)  | When delivery confirmed      |
| correlation_id             | VARCHAR(64)  | For idempotency              |

**Table:** `notifications.delivery_attempts`

| Column                  | Type          | Description                 |
|-------------------------|---------------|-----------------------------|
| id                      | INT           | Auto-increment PK           |
| notification_request_id | CHAR(26)      | FK to notification_requests |
| channel                 | INT           | Channel enum                |
| status                  | INT           | Delivery status enum        |
| attempted_at            | DATETIME(6)   | When attempted              |
| attempt_number          | INT           | Attempt sequence            |
| provider_message_id     | VARCHAR(256)  | Provider's tracking ID      |
| failure_reason          | VARCHAR(1024) | Error message               |
| next_retry_after        | TIME(6)       | Backoff duration            |

**Indexes:**

- `notification_requests`: customer_id, order_id, status, (status, created_at), correlation_id
- `delivery_attempts`: notification_request_id, attempted_at

### 3.3 API Surface

#### 3.3.1 SLA Clocks

```
GET  /api/clocks/sla?orderId={id}&kind={kind}     # Query clocks
GET  /api/clocks/sla/{clockId}                     # Get clock details
POST /api/clocks/sla/{clockId}/pause               # Pause clock (ops)
POST /api/clocks/sla/{clockId}/resume              # Resume clock (ops)
```

#### 3.3.2 Notifications

```
GET  /api/notifications?orderId={id}&status={status}  # Query notifications
GET  /api/notifications/{notificationId}              # Get notification details
POST /api/notifications/{notificationId}/retry        # Retry failed notification (ops)
```

#### 3.3.3 SSE Extensions

Extend `/api/orders/changes` to include:

- `clock.at_risk` events
- `clock.breached` events
- `notification.failed` events (ops channel)

### 3.4 Projection Runners

Add CLI commands to `Holmes.Projections.Runner`:

```bash
dotnet run --project src/Tools/Holmes.Projections.Runner --projection sla-clocks [--reset true]
dotnet run --project src/Tools/Holmes.Projections.Runner --projection compliance-grants [--reset true]
dotnet run --project src/Tools/Holmes.Projections.Runner --projection notification-history [--reset true]
```

## 4. Acceptance Checklist

1. **SLA Clocks:**
    - [x] Clock starts when order enters tracked state
    - [x] Business calendar correctly computes deadlines with holidays
    - [x] At-risk threshold fires event at 80% elapsed
    - [x] Breach fires event when deadline exceeded
    - [x] Pause/resume correctly adjusts remaining time
    - [ ] Clock projection replayable and queryable

2. **Notifications:**
    - [x] `NotificationCreated` events emitted for configured triggers
    - [x] Provider abstraction delivers via email/SMS/webhook stubs
    - [x] Delivery status tracked through lifecycle
    - [x] Failed notifications retryable with backoff
    - [ ] Notification history projection replayable

3. **Observability:**
    - [ ] Clock health dashboard shows at-risk/breached counts
    - [ ] Notification delivery metrics visible
    - [ ] Alerts configured for breached clocks
    - [ ] Alerts configured for notification failures

4. **Documentation:**
    - [ ] API contracts published
    - [ ] Runbooks updated with clock/notification scenarios

## 5. Risks & Mitigations

- **Clock Drift:** Watchdog service must handle server restarts gracefully; use persistent checkpoint for last-checked
  position; add leader election if horizontally scaled.
- **Notification Provider Failures:** Implement circuit breaker pattern; capture all failures for retry; alert on
  sustained failure rates.
- **Calendar Edge Cases:** Business-day math is notoriously tricky; add extensive property-based tests for holiday
  boundaries, timezone transitions, and leap years.

## 6. Dependencies

- **From Phase 2:** Order aggregate, Intake completion events, SSE infrastructure

## 7. Non-Goals (Phase 3)

- Compliance policy gating (Phase 3.2)
- Adverse action clocks (Phase 3.2 / Phase 4)
- Service-level SLA clocks (Phase 3.1)
- Direct email/SMS sending by Holmes (always provider-delegated)
- Multi-region calendar support (design-ready, implement Phase 4+)
- Notification template authoring UI (API-only in Phase 3)

## 8. Current Status Snapshot

*[Updated 2025-12-07]*

- **SLA Clocks:** ✅ Module complete
    - Domain: `SlaClock` aggregate with `ClockKind` (Intake/Fulfillment/Overall), `ClockState`, domain events
    - Application: Commands (`Start`, `Pause`, `Resume`, `MarkAtRisk`, `MarkBreached`, `Complete`), queries,
      `OrderStatusChangedSlaHandler`
    - Infrastructure: `SlaClockDbContext`, repository with `SlaClockMapper`, `BusinessCalendarService` (US Federal
      holidays 2024-2026)
    - Watchdog: `SlaClockWatchdogService` background service for at-risk/breach detection
    - Integration: `SlaClockAtRiskNotificationHandler`, `SlaClockBreachedNotificationHandler`
    - Tests: Domain unit tests (`SlaClockTests`, `BusinessCalendarServiceTests`), background service tests (
      `SlaClockWatchdogServiceTests`)
    - Default SLAs: Intake 1 day, Fulfillment 3 days, Overall 5 days (customer-defined via service agreements)
- **Notifications:** ✅ Module complete
    - Domain: `Notification` aggregate, enums, value objects, events, repository interface
    - Application: Commands (`Create`, `Process`, `RecordDeliveryResult`), query, event handlers
    - Infrastructure: DbContext, repository with `NotificationMapper`, stub providers (`LoggingEmailProvider`,
      `LoggingSmsProvider`, `LoggingWebhookProvider`)
    - Background: `NotificationProcessingService` for polling-based delivery
    - Tests: Domain unit tests (`NotificationTests`), background service tests (
      `NotificationProcessingServiceTests`)
    - Added to solution under `src/Modules/Notifications/`
- **Identity Broker:** ✅ Complete (Holmes.Identity.Server with Duende IdentityServer)

## 9. Next Actions

1. Build projection read models for `sla_clocks` and `notifications_history`
2. Add observability dashboards for clock health and notification delivery
3. Proceed to Phase 3.1: Services & Fulfillment module

---

This document is the authoritative reference for Phase 3 delivery; update it at each checkpoint to reflect decisions,
scope changes, and readiness status.

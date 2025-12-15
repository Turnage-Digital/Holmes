# Holmes Audit & Event Sourcing Guide

This document describes how Holmes captures and stores domain events for audit, compliance, and event replay purposes.

## Overview

Every mutation to domain aggregates (Order, Subject, User, Customer, IntakeSession, ServiceRequest, etc.) raises a
domain event. These events are persisted to the `event_records` table within the same database transaction as the
aggregate state change, ensuring consistency.

This provides:

- **Complete audit trail** of all changes to any entity
- **Point-in-time reconstruction** of entity state
- **Compliance evidence** for regulatory requirements
- **Event replay** capability for rebuilding read models

## Event Record Schema

```sql
CREATE TABLE event_records (
    position        BIGINT AUTO_INCREMENT PRIMARY KEY,
    tenant_id       VARCHAR(64) NOT NULL,
    stream_id       VARCHAR(256) NOT NULL,
    stream_type     VARCHAR(128) NOT NULL,
    version         BIGINT NOT NULL,
    event_id        VARCHAR(64) NOT NULL,
    name            VARCHAR(256) NOT NULL,
    payload         JSON NOT NULL,
    metadata        JSON,
    created_at      DATETIME(6) NOT NULL,
    correlation_id  VARCHAR(64),
    causation_id    VARCHAR(64),
    actor_id        VARCHAR(256),
    idempotency_key VARCHAR(512) NOT NULL UNIQUE
);
```

### Column Reference

| Column            | Description                                 | Audit Use                          |
|-------------------|---------------------------------------------|------------------------------------|
| `position`        | Global sequence number                      | Ordering events across all streams |
| `tenant_id`       | Customer organization ID                    | Multi-tenant isolation             |
| `stream_id`       | Entity identifier (e.g., `Order:01HXYZ...`) | Query all events for an entity     |
| `stream_type`     | Aggregate type (e.g., `Order`, `Subject`)   | Query by entity type               |
| `version`         | Per-stream sequence number                  | Optimistic concurrency             |
| `event_id`        | Unique event identifier (ULID)              | Deduplication                      |
| `name`            | Event type (e.g., `OrderStatusChanged`)     | Filter by event type               |
| `payload`         | Full event data as JSON                     | The actual change details          |
| `metadata`        | Optional additional context                 | Custom audit metadata              |
| `created_at`      | Event timestamp (UTC)                       | When the change occurred           |
| `correlation_id`  | Distributed trace ID                        | Links to HTTP request/trace        |
| `causation_id`    | Parent span ID                              | What triggered this event          |
| `actor_id`        | User who caused the change                  | WHO made this change               |
| `idempotency_key` | Prevents duplicate writes                   | Exactly-once persistence           |

## Stream Types and Events

### Order (Workflow)

Stream ID format: `Order:{orderId}`

| Event                | Description                      | Key Payload Fields                                              |
|----------------------|----------------------------------|-----------------------------------------------------------------|
| `OrderStatusChanged` | Order transitioned to new status | `OrderId`, `PreviousStatus`, `NewStatus`, `Reason`, `ChangedAt` |

### Subject

Stream ID format: `Subject:{subjectId}`

| Event                    | Description                 | Key Payload Fields                                                          |
|--------------------------|-----------------------------|-----------------------------------------------------------------------------|
| `SubjectRegistered`      | New subject created         | `SubjectId`, `GivenName`, `FamilyName`, `DateOfBirth`, `Email`              |
| `SubjectDataUpdated`     | Subject PII updated         | `SubjectId`, `GivenName`, `FamilyName`, `DateOfBirth`, `Email`, `UpdatedAt` |
| `SubjectAddressAdded`    | Address added to subject    | `SubjectId`, `Address`                                                      |
| `SubjectEmploymentAdded` | Employment record added     | `SubjectId`, `Employment`                                                   |
| `SubjectEducationAdded`  | Education record added      | `SubjectId`, `Education`                                                    |
| `SubjectAliasAdded`      | Alias/AKA added             | `SubjectId`, `Alias`                                                        |
| `SubjectMerged`          | Subject merged into another | `SourceSubjectId`, `TargetSubjectId`, `MergedAt`                            |

### User

Stream ID format: `User:{userId}`

| Event                | Description                 | Key Payload Fields                                                    |
|----------------------|-----------------------------|-----------------------------------------------------------------------|
| `UserInvited`        | User invited to system      | `UserId`, `Email`, `DisplayName`, `InvitedAt`                         |
| `UserRegistered`     | User completed registration | `UserId`, `Email`, `DisplayName`, `Issuer`, `Subject`, `RegisteredAt` |
| `UserProfileUpdated` | User profile changed        | `UserId`, `Email`, `DisplayName`, `UpdatedAt`                         |
| `UserSuspended`      | User account suspended      | `UserId`, `SuspendedAt`, `Reason`                                     |
| `UserReactivated`    | User account reactivated    | `UserId`, `ReactivatedAt`                                             |
| `UserRoleGranted`    | Role assigned to user       | `UserId`, `Role`, `GrantedAt`                                         |
| `UserRoleRevoked`    | Role removed from user      | `UserId`, `Role`, `RevokedAt`                                         |

### Customer

Stream ID format: `Customer:{customerId}`

| Event                   | Description                  | Key Payload Fields                   |
|-------------------------|------------------------------|--------------------------------------|
| `CustomerRegistered`    | New customer org created     | `CustomerId`, `Name`, `RegisteredAt` |
| `CustomerRenamed`       | Customer name changed        | `CustomerId`, `Name`, `RenamedAt`    |
| `CustomerSuspended`     | Customer account suspended   | `CustomerId`, `SuspendedAt`          |
| `CustomerReactivated`   | Customer account reactivated | `CustomerId`, `ReactivatedAt`        |
| `CustomerAdminAssigned` | Admin added to customer      | `CustomerId`, `UserId`, `AssignedAt` |
| `CustomerAdminRemoved`  | Admin removed from customer  | `CustomerId`, `UserId`, `RemovedAt`  |

### IntakeSession

Stream ID format: `IntakeSession:{sessionId}`

| Event                      | Description               | Key Payload Fields                                     |
|----------------------------|---------------------------|--------------------------------------------------------|
| `IntakeSessionInvited`     | Intake invite sent        | `IntakeSessionId`, `OrderId`, `SubjectId`, `ExpiresAt` |
| `IntakeSessionStarted`     | Subject began intake      | `IntakeSessionId`, `StartedAt`                         |
| `IntakeProgressSaved`      | Progress checkpoint saved | `IntakeSessionId`, `AnswersSnapshot`                   |
| `ConsentCaptured`          | Consent form signed       | `IntakeSessionId`, `Artifact`                          |
| `IntakeSubmissionReceived` | Intake form submitted     | `IntakeSessionId`, `SubmittedAt`                       |
| `IntakeSubmissionAccepted` | Submission approved       | `IntakeSessionId`, `AcceptedAt`                        |
| `IntakeSessionExpired`     | Session timed out         | `IntakeSessionId`, `ExpiredAt`, `Reason`               |
| `IntakeSessionSuperseded`  | Replaced by new session   | `IntakeSessionId`, `SupersededByIntakeSessionId`       |

### ServiceRequest

Stream ID format: `ServiceRequest:{requestId}`

| Event                      | Description             | Key Payload Fields                               |
|----------------------------|-------------------------|--------------------------------------------------|
| `ServiceRequestCreated`    | Service request created | `ServiceRequestId`, `OrderId`, `ServiceType`     |
| `ServiceRequestDispatched` | Sent to vendor          | `ServiceRequestId`, `DispatchedAt`               |
| `ServiceRequestInProgress` | Vendor processing       | `ServiceRequestId`, `StartedAt`                  |
| `ServiceRequestCompleted`  | Service completed       | `ServiceRequestId`, `CompletedAt`, `Result`      |
| `ServiceRequestFailed`     | Service failed          | `ServiceRequestId`, `FailedAt`, `Reason`         |
| `ServiceRequestRetried`    | Retry attempted         | `ServiceRequestId`, `RetriedAt`, `AttemptNumber` |
| `ServiceRequestCanceled`   | Service canceled        | `ServiceRequestId`, `CanceledAt`, `Reason`       |

### SlaClock

Stream ID format: `SlaClock:{clockId}`

| Event               | Description          | Key Payload Fields                 |
|---------------------|----------------------|------------------------------------|
| `SlaClockStarted`   | SLA timer started    | `SlaClockId`, `OrderId`, `DueAt`   |
| `SlaClockPaused`    | Timer paused         | `SlaClockId`, `PausedAt`, `Reason` |
| `SlaClockResumed`   | Timer resumed        | `SlaClockId`, `ResumedAt`          |
| `SlaClockAtRisk`    | Approaching deadline | `SlaClockId`, `AtRiskAt`           |
| `SlaClockBreached`  | Deadline missed      | `SlaClockId`, `BreachedAt`         |
| `SlaClockCompleted` | Completed within SLA | `SlaClockId`, `CompletedAt`        |

### Notification

Stream ID format: `NotificationRequest:{requestId}`

| Event                        | Description            | Key Payload Fields                              |
|------------------------------|------------------------|-------------------------------------------------|
| `NotificationRequestCreated` | Notification queued    | `NotificationRequestId`, `Channel`, `Recipient` |
| `NotificationQueued`         | Sent to delivery queue | `NotificationRequestId`, `QueuedAt`             |
| `NotificationDelivered`      | Successfully delivered | `NotificationRequestId`, `DeliveredAt`          |
| `NotificationBounced`        | Delivery bounced       | `NotificationRequestId`, `BouncedAt`, `Reason`  |
| `NotificationDeliveryFailed` | Delivery failed        | `NotificationRequestId`, `FailedAt`, `Reason`   |
| `NotificationCancelled`      | Notification canceled  | `NotificationRequestId`, `CancelledAt`          |

## Common Audit Queries

### All events for a specific entity

```sql
SELECT
    position,
    name AS event,
    payload,
    created_at,
    actor_id
FROM event_records
WHERE stream_id = 'Order:01HXYZ...'
ORDER BY position;
```

### Entity history with human-readable details

```sql
SELECT
    position,
    name AS event,
    JSON_UNQUOTE(JSON_EXTRACT(payload, '$.Reason')) AS reason,
    created_at,
    actor_id AS changed_by
FROM event_records
WHERE stream_id = 'Order:01HXYZ...'
ORDER BY position;
```

### All changes made by a specific user

```sql
SELECT
    stream_id,
    stream_type,
    name AS event,
    created_at,
    payload
FROM event_records
WHERE actor_id = 'user-id-here'
ORDER BY created_at DESC
LIMIT 100;
```

### All events of a specific type in a date range

```sql
SELECT
    stream_id,
    payload,
    created_at,
    actor_id
FROM event_records
WHERE name = 'SubjectDataUpdated'
  AND created_at BETWEEN '2025-01-01' AND '2025-12-31'
ORDER BY created_at;
```

### Events by stream type (all Orders, all Subjects, etc.)

```sql
SELECT
    stream_id,
    name AS event,
    created_at,
    actor_id
FROM event_records
WHERE stream_type = 'Subject'
  AND created_at >= '2025-01-01'
ORDER BY created_at;
```

### Correlated events (all events from one HTTP request)

```sql
SELECT
    stream_id,
    stream_type,
    name AS event,
    created_at
FROM event_records
WHERE correlation_id = 'trace-id-from-request'
ORDER BY position;
```

### Subject PII change history

```sql
SELECT
    created_at,
    name AS event,
    JSON_UNQUOTE(JSON_EXTRACT(payload, '$.GivenName')) AS given_name,
    JSON_UNQUOTE(JSON_EXTRACT(payload, '$.FamilyName')) AS family_name,
    JSON_UNQUOTE(JSON_EXTRACT(payload, '$.Email')) AS email,
    actor_id AS changed_by
FROM event_records
WHERE stream_id = 'Subject:01HABC...'
  AND name IN ('SubjectRegistered', 'SubjectDataUpdated')
ORDER BY position;
```

### Count events by type (activity metrics)

```sql
SELECT
    name AS event_type,
    COUNT(*) AS count,
    DATE(created_at) AS date
FROM event_records
WHERE created_at >= DATE_SUB(NOW(), INTERVAL 30 DAY)
GROUP BY name, DATE(created_at)
ORDER BY date DESC, count DESC;
```

## Event Replay & Projections

Events can be replayed to rebuild read models (projections) using the projection runner tool:

```bash
# Replay from events (reads from event_records table)
dotnet run --project src/Tools/Holmes.Projections.Runner -- \
  --projection subject \
  --from-events true \
  --reset true \
  --batch-size 500

# Available projections:
# user, customer, subject, intake-sessions, order-summary, order-timeline
```

### Projection Checkpoints

The `projection_checkpoints` table tracks replay progress:

```sql
SELECT
    projection_name,
    position AS last_processed_position,
    updated_at AS last_run
FROM projection_checkpoints
WHERE tenant_id = '*';
```

## Data Retention

Event records are immutable and should be retained according to your compliance requirements:

- **SOC 2**: Typically 1 year minimum
- **HIPAA**: 6 years from creation or last effective date
- **GDPR**: As long as necessary for the purpose; subject to right of erasure

For GDPR right-to-erasure requests affecting event data, consider:

1. Pseudonymization of PII in event payloads
2. Logical deletion markers rather than physical deletion
3. Consult legal counsel for your specific jurisdiction

## Architecture Notes

### Event Persistence Flow

1. Application code modifies an aggregate (e.g., `order.TransitionTo(...)`)
2. Aggregate raises domain event(s) internally
3. `UnitOfWork.SaveChangesAsync()` is called
4. Within a database transaction:
    - Aggregate state saved to its table
    - Events serialized and written to `event_records`
5. Transaction commits (atomic)
6. Events dispatched via MediatR to handlers (projections, notifications, etc.)

### Guarantees

- **Atomicity**: Events are persisted in the same transaction as state changes
- **Ordering**: `position` column provides global ordering; `version` provides per-stream ordering
- **Idempotency**: `idempotency_key` prevents duplicate event writes
- **Consistency**: If the transaction fails, neither state nor events are persisted

### Actor Identification

The `actor_id` is extracted from the authenticated user's JWT claims:

- `sub` claim (OpenID Connect standard)
- `NameIdentifier` claim (legacy)

For system-initiated changes (background jobs, etc.), `actor_id` may be null or a system identifier.

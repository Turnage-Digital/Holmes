# Holmes State Machines and Lifecycle (Current State)

**Last Updated:** 2025-12-23

This document matches the current code under `src/Modules`.

---

## Executive Summary

```
ORDER LIFECYCLE (Workflow)

Created -> Invited -> IntakeInProgress -> IntakeComplete -> ReadyForFulfillment
                                                           |
                                                           v
                                               FulfillmentInProgress
                                                           |
                                                           v
                                                    ReadyForReport -> Closed

Global interrupts:
- Blocked (pause and resume to previous status)
- Canceled (terminal)

INTAKE SESSION (Intake)

Invited -> InProgress -> AwaitingReview -> Submitted
   |                           |
   +---------------------------+-> Abandoned (expired or superseded)

SERVICE REQUEST (Services)

Pending -> Dispatched -> InProgress -> Completed
                      \             \
                       \-> Failed -> Retry -> Pending
                         -> Canceled
```

---

## 1) Order Status Flow

**Location:** `src/Modules/Orders/Holmes.Orders.Domain/Order.cs`

```
Created -> Invited -> IntakeInProgress -> IntakeComplete -> ReadyForFulfillment
                                                                       |
                                                                       v
                                                                FulfillmentInProgress
                                                                       |
                                                                       v
                                                                ReadyForReport -> Closed

Blocked (pause)  <->  Resume to previous status
Canceled (terminal)
```

### Transitions

| From                    | To                      | Trigger/Command                       | Notes                                                     |
|-------------------------|-------------------------|---------------------------------------|-----------------------------------------------------------|
| `Created`               | `Invited`               | `RecordOrderInviteCommand`            | Issued by `IntakeSessionInvitedIntegrationEvent` handler. |
| `Invited`               | `IntakeInProgress`      | `MarkOrderIntakeStartedCommand`       | Issued by `IntakeSessionStartedIntegrationEvent` handler. |
| `IntakeInProgress`      | `IntakeComplete`        | `MarkOrderIntakeSubmittedCommand`     | Issued by `IntakeSubmissionWorkflowHandler`.              |
| `IntakeComplete`        | `ReadyForFulfillment`   | `MarkOrderReadyForFulfillmentCommand` | Issued by `IntakeSubmissionWorkflowHandler`.              |
| `ReadyForFulfillment`   | `FulfillmentInProgress` | `BeginOrderFulfillmentCommand`        | Issued after creating service requests.                   |
| `FulfillmentInProgress` | `ReadyForReport`        | `MarkOrderReadyForReportCommand`      | Issued after all services complete.                       |
| `ReadyForReport`        | `Closed`                | `CloseOrderCommand`                   | Manual or system close.                                   |
| Any non-terminal        | `Blocked`               | `BlockOrderCommand`                   | Stores `BlockedFromStatus`.                               |
| `Blocked`               | Previous status         | `ResumeOrderFromBlockCommand`         | Returns to `BlockedFromStatus`.                           |
| Any non-terminal        | `Canceled`              | `CancelOrderCommand`                  | Terminal.                                                 |

**Guards**

- Blocked orders cannot progress until resumed.
- Canceled and Closed are terminal.
- Intake transitions require the active IntakeSession ID.

---

## 2) IntakeSession Status Flow

**Location:** `src/Modules/IntakeSessions/Holmes.IntakeSessions.Domain/IntakeSession.cs`

```
Invited -> InProgress -> AwaitingReview -> Submitted
   |                           |
   +---------------------------+-> Abandoned (expired or superseded)
```

### Transitions

| From               | To               | Trigger/Command                 | Notes                                    |
|--------------------|------------------|---------------------------------|------------------------------------------|
| (new)              | `Invited`        | `IssueIntakeInviteCommand`      | Creates resume token + TTL.              |
| `Invited`          | `InProgress`     | `StartIntakeSessionCommand`     | Must be unexpired.                       |
| `InProgress`       | `AwaitingReview` | `SubmitIntakeCommand`           | Requires consent + answers.              |
| `AwaitingReview`   | `Submitted`      | `AcceptIntakeSubmissionCommand` | Moves Order to `ReadyForFulfillment`.    |
| Any (non-terminal) | `Abandoned`      | `Expire()` or `Supersede()`     | Expired TTL or superseded by new invite. |

**Guards**

- Submit requires `ConsentArtifact` and `AnswersSnapshot`.
- Supersede is not allowed after `Submitted`.

---

## 3) Service Request Status Flow

**Location:** `src/Modules/Services/Holmes.Services.Domain/Service.cs`

Note: The aggregate is named `Service`, but the stream type and events are `Service`.

```
Pending -> Dispatched -> InProgress -> Completed
                      \             \
                       \-> Failed -> Retry -> Pending
                         -> Canceled
```

### Transitions

| From             | To           | Trigger/Command                | Notes                                 |
|------------------|--------------|--------------------------------|---------------------------------------|
| (new)            | `Pending`    | `CreateServiceCommand`         | Created by `OrderFulfillmentHandler`. |
| `Pending`        | `Dispatched` | `DispatchServiceCommand`       | Requires vendor assignment.           |
| `Dispatched`     | `InProgress` | `ProcessVendorCallbackCommand` | Idempotent.                           |
| `InProgress`     | `Completed`  | `RecordServiceResultCommand`   | Terminal.                             |
| `InProgress`     | `Failed`     | `ProcessVendorCallbackCommand` | Terminal unless retried.              |
| `Failed`         | `Pending`    | `RetryServiceCommand`          | Only if attempts < max.               |
| Any non-terminal | `Canceled`   | `CancelServiceCommand`         | Terminal.                             |

---

## 4) SLA Clock Lifecycle

**Location:** `src/Modules/SlaClocks/Holmes.SlaClocks.Domain/SlaClock.cs`

Clock kinds today: `Overall`, `Intake`, `Fulfillment` (`Custom` reserved for future).

```
Running -> AtRisk -> Breached
   |        |
   |        +-- (if paused, state stays Paused but AtRiskAt is recorded)
   |
   +-> Paused -> Resume -> Running/AtRisk
   |
   +-> Completed
```

**Triggers**

- `OrderStatusChangedSlaHandler` starts/completes clocks on order status changes.
- `SlaClockWatchdogService` marks AtRisk/Breached based on thresholds.

---

## 5) Cross-Aggregate Choreography

**Event handlers in consuming module Applications**

- `IntakeToWorkflowHandler` (`src/Modules/Orders/Holmes.Orders.Application/EventHandlers/IntakeToWorkflowHandler.cs`)
    - `IntakeSessionInvitedIntegrationEvent` -> `RecordOrderInviteCommand`
    - `IntakeSessionStartedIntegrationEvent` -> `MarkOrderIntakeStartedCommand`

- `IntakeSubmissionWorkflowHandler` (
  `src/Modules/Orders/Holmes.Orders.Application/EventHandlers/IntakeSubmissionWorkflowHandler.cs`)
    - `IntakeSubmissionReceivedIntegrationEvent` -> `MarkOrderIntakeSubmittedCommand`
    - `IntakeSubmissionAcceptedIntegrationEvent` -> `MarkOrderReadyForFulfillmentCommand`

- `OrderStatusChangedSlaHandler` (
  `src/Modules/SlaClocks/Holmes.SlaClocks.Application/EventHandlers/OrderStatusChangedSlaHandler.cs`)
    - `OrderStatusChangedIntegrationEvent` starts/completes/pauses/resumes SLA clocks.

- `OrderFulfillmentHandler` (
  `src/Modules/Services/Holmes.Services.Application/EventHandlers/OrderFulfillmentHandler.cs`)
    - `OrderStatusChangedIntegrationEvent` creates service requests from the customer catalog.
    - Calls `BeginOrderFulfillmentCommand` if any were created.

- `ServiceCompletionOrderHandler` (
  `src/Modules/Orders/Holmes.Orders.Application/EventHandlers/ServiceCompletionOrderHandler.cs`)
    - `ServiceCompletedIntegrationEvent` -> when all services are completed, calls `MarkOrderReadyForReportCommand`.

- Notifications (`src/Modules/Notifications/Holmes.Notifications.Application/EventHandlers/*.cs`)
    - `IntakeSessionInvitedIntegrationEvent` -> `IntakeInviteNotificationHandler` sends invite emails.
    - `SlaClockAtRiskIntegrationEvent` and `SlaClockBreachedIntegrationEvent` log and are ready to wire to
      notification requests.

---

## 6) Aggregate Existence Timeline

| Order Status            | IntakeSession Exists? | Service Requests Exist?         | SLA Clocks                                 |
|-------------------------|-----------------------|---------------------------------|--------------------------------------------|
| `Created`               | No                    | No                              | Overall (Running)                          |
| `Invited`               | Yes (Invited)         | No                              | Overall, Intake (Running)                  |
| `IntakeInProgress`      | Yes (InProgress)      | No                              | Overall, Intake (Running)                  |
| `IntakeComplete`        | Yes (AwaitingReview)  | No                              | Overall (Running), Intake (Completed)      |
| `ReadyForFulfillment`   | Yes (Submitted)       | Created if catalog has services | Overall, Fulfillment (Running)             |
| `FulfillmentInProgress` | Yes (Submitted)       | Yes (Pending/InProgress)        | Overall, Fulfillment (Running)             |
| `ReadyForReport`        | Yes (Submitted)       | Yes (Completed)                 | Overall (Running), Fulfillment (Completed) |
| `Closed`                | Yes (Submitted)       | Yes (Completed)                 | All (Completed)                            |

---

## 7) Happy Path (Current)

1. Order created -> `OrderStatusChangedIntegrationEvent` starts Overall clock.
2. Intake session invited -> Order moves to `Invited` and Intake clock starts.
3. Subject starts intake -> Order moves to `IntakeInProgress`.
4. Subject submits intake -> Order moves to `IntakeComplete`.
5. Intake accepted -> Order moves to `ReadyForFulfillment`.
6. Services created -> Order moves to `FulfillmentInProgress`.
7. All services complete -> Order moves to `ReadyForReport`.
8. Operator closes order -> Order moves to `Closed`, Overall clock completes.

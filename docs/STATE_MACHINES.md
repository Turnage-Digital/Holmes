# Holmes State Machines & Lifecycle Documentation

This document describes the state machines for the three primary aggregates (Order, IntakeSession, ServiceRequest) and
how they interact.

---

## Executive Summary

```
┌─────────────────────────────────────────────────────────────────────────────────┐
│                              ORDER LIFECYCLE                                     │
│                                                                                 │
│  Created ──► Invited ──► IntakeInProgress ──► IntakeComplete ──► ReadyFor...   │
│     │           │              │                    │              Fulfillment  │
│     │           ▼              ▼                    ▼                   │       │
│     │     IntakeSession   IntakeSession       IntakeSession             │       │
│     │       Created        Started             Submitted                │       │
│     │       (Invited)     (InProgress)       (AwaitingReview)           │       │
│     │                                              │                    │       │
│     │                                              ▼                    │       │
│     │                                    AcceptSubmission               │       │
│     │                                   (IntakeSession→Submitted)       │       │
│     │                                              │                    │       │
│     │                                              ▼                    │       │
│     │                                   Order→ReadyForFulfillment ◄─────┘       │
│     │                                              │                            │
│     │                                              ▼                            │
│     │                                    ServiceRequests Created                │
│     │                                              │                            │
│     │                                              ▼                            │
│     │                                   Order→FulfillmentInProgress             │
│     │                                              │                            │
│     ▼                                              ▼                            │
│  SLA Clock                              All Services Completed                  │
│  (Overall)                                         │                            │
│   Started                                          ▼                            │
│                                          Order→ReadyForReport                   │
│                                                    │                            │
│                                                    ▼                            │
│                                              Order→Closed                       │
└─────────────────────────────────────────────────────────────────────────────────┘
```

---

## 1. Order Status Flow

**Location:** `src/Modules/Workflow/Holmes.Workflow.Domain/Order.cs`

```
Created ──► Invited ──► IntakeInProgress ──► IntakeComplete ──► ReadyForFulfillment
                                                                       │
                                                                       ▼
Canceled ◄── Blocked ◄─────────────────── FulfillmentInProgress ◄──────┘
                                                 │
                                                 ▼
                                          ReadyForReport ──► Closed
```

### Transitions

| From                    | To                      | Trigger                     | Handler/Command                       | What Happens                                    |
|-------------------------|-------------------------|-----------------------------|---------------------------------------|-------------------------------------------------|
| `Created`               | `Invited`               | `RecordInvite()`            | `RecordOrderInviteCommand`            | IntakeSession created first, then Order updated |
| `Invited`               | `IntakeInProgress`      | `MarkIntakeInProgress()`    | `MarkOrderIntakeStartedCommand`       | Subject opened intake form                      |
| `IntakeInProgress`      | `IntakeComplete`        | `MarkIntakeSubmitted()`     | `MarkOrderIntakeSubmittedCommand`     | Subject submitted intake form                   |
| `IntakeComplete`        | `ReadyForFulfillment`   | `MarkReadyForFulfillment()` | `MarkOrderReadyForFulfillmentCommand` | Intake accepted by system                       |
| `ReadyForFulfillment`   | `FulfillmentInProgress` | `BeginFulfillment()`        | `BeginOrderFulfillmentCommand`        | Services created and dispatched                 |
| `FulfillmentInProgress` | `ReadyForReport`        | `MarkReadyForReport()`      | `MarkOrderReadyForReportCommand`      | All services completed                          |
| `ReadyForReport`        | `Closed`                | `Close()`                   | `CloseOrderCommand`                   | Report delivered, order finalized               |
| Any (non-terminal)      | `Blocked`               | `Block()`                   | `BlockOrderCommand`                   | Order paused due to issue                       |
| `Blocked`               | Previous                | `ResumeFromBlock()`         | `ResumeOrderCommand`                  | Issue resolved                                  |
| Any (non-terminal)      | `Canceled`              | `Cancel()`                  | `CancelOrderCommand`                  | Order abandoned                                 |

---

## 2. IntakeSession Status Flow

**Location:** `src/Modules/Intake/Holmes.Intake.Domain/IntakeSession.cs`

```
Invited ──► InProgress ──► AwaitingReview ──► Submitted
    │           │               │
    └───────────┴───────────────┴──► Abandoned (expired/superseded)
```

### Transitions

| From               | To               | Trigger                     | Handler/Command                 | What Happens                                 |
|--------------------|------------------|-----------------------------|---------------------------------|----------------------------------------------|
| (new)              | `Invited`        | `IntakeSession.Invite()`    | `IssueIntakeInviteCommand`      | Session created, resume token generated      |
| `Invited`          | `InProgress`     | `Start()`                   | `StartIntakeSessionCommand`     | Subject opened form (via resume token)       |
| `InProgress`       | `AwaitingReview` | `Submit()`                  | `SubmitIntakeCommand`           | Subject submitted, data persisted to Subject |
| `AwaitingReview`   | `Submitted`      | `AcceptSubmission()`        | `AcceptIntakeSubmissionCommand` | System accepted, Order→ReadyForFulfillment   |
| Any (non-terminal) | `Abandoned`      | `Expire()` or `Supersede()` | Watchdog or new invite          | Session invalidated                          |

---

## 3. ServiceRequest Status Flow

**Location:** `src/Modules/Services/Holmes.Services.Domain/ServiceRequest.cs`

```
Pending ──► Dispatched ──► InProgress ──► Completed
                │              │
                └──────────────┴──► Failed ──► (Retry) ──► Pending
                                       │
                                       └──► Canceled
```

### Transitions

| From               | To           | Trigger                   | Handler/Command                 | What Happens               |
|--------------------|--------------|---------------------------|---------------------------------|----------------------------|
| (new)              | `Pending`    | `ServiceRequest.Create()` | `CreateServiceRequestCommand`   | Request created for vendor |
| `Pending`          | `Dispatched` | `Dispatch()`              | `DispatchServiceRequestCommand` | Sent to vendor             |
| `Dispatched`       | `InProgress` | `MarkInProgress()`        | `ProcessVendorCallbackCommand`  | Vendor acknowledged        |
| `InProgress`       | `Completed`  | `RecordResult()`          | `RecordServiceResultCommand`    | Vendor returned results    |
| `InProgress`       | `Failed`     | `Fail()`                  | `ProcessVendorCallbackCommand`  | Vendor error               |
| `Failed`           | `Pending`    | `Retry()`                 | `RetryServiceRequestCommand`    | Manual or auto retry       |
| Any (non-terminal) | `Canceled`   | `Cancel()`                | `CancelServiceRequestCommand`   | Request abandoned          |

---

## 4. SLA Clock Lifecycle

**Location:** `src/Modules/SlaClocks/Holmes.SlaClocks.Domain/SlaClock.cs`

SLA Clocks are created in response to Order status changes. They track time-to-completion for different phases.

### Clock Types

| ClockKind     | Started When                | Completed When           |
|---------------|-----------------------------|--------------------------|
| `Overall`     | Order → Created             | Order → Closed           |
| `Intake`      | Order → Invited             | Order → IntakeComplete   |
| `Fulfillment` | Order → ReadyForFulfillment | Order → ReadyForReport   |
| `Service`     | ServiceRequest dispatched   | ServiceRequest completed |

### Clock States

```
Running ──► AtRisk ──► Breached
    │          │
    └──► Paused (Order Blocked) ──► Resume (Unblocked)
    │          │
    └──────────┴──► Completed
```

---

## 5. Cross-Aggregate Event Choreography

The following handlers in `src/Holmes.App.Integration/EventHandlers/` orchestrate cross-module behavior:

### IntakeToWorkflowHandler

**Listens:** `IntakeSessionInvited`, `IntakeSessionStarted`

| Event                  | Action                                                                    |
|------------------------|---------------------------------------------------------------------------|
| `IntakeSessionInvited` | Sends `RecordOrderInviteCommand` → Order moves to `Invited`               |
| `IntakeSessionStarted` | Sends `MarkOrderIntakeStartedCommand` → Order moves to `IntakeInProgress` |

### OrderWorkflowGateway

**Called by:** `SubmitIntakeCommand`, `AcceptIntakeSubmissionCommand`

| Method                         | Action                                                                             |
|--------------------------------|------------------------------------------------------------------------------------|
| `NotifyIntakeSubmittedAsync()` | Sends `MarkOrderIntakeSubmittedCommand` → Order moves to `IntakeComplete`          |
| `NotifyIntakeAcceptedAsync()`  | Sends `MarkOrderReadyForFulfillmentCommand` → Order moves to `ReadyForFulfillment` |

### OrderStatusChangedSlaHandler

**Listens:** `OrderStatusChanged`

| Order Status          | SLA Action                   |
|-----------------------|------------------------------|
| `Created`             | Start `Overall` clock        |
| `Invited`             | Start `Intake` clock         |
| `IntakeComplete`      | Complete `Intake` clock      |
| `ReadyForFulfillment` | Start `Fulfillment` clock    |
| `ReadyForReport`      | Complete `Fulfillment` clock |
| `Closed`              | Complete `Overall` clock     |
| `Blocked`             | Pause all active clocks      |
| `Canceled`            | Complete all active clocks   |

### OrderFulfillmentHandler

**Listens:** `OrderStatusChanged` (when status = `ReadyForFulfillment`)

| Action                                                                          |
|---------------------------------------------------------------------------------|
| 1. Query customer's service catalog                                             |
| 2. Create `ServiceRequest` for each enabled service                             |
| 3. Send `BeginOrderFulfillmentCommand` → Order moves to `FulfillmentInProgress` |

### ServiceCompletionOrderHandler

**Listens:** `ServiceRequestCompleted`

| Condition              | Action                                                                  |
|------------------------|-------------------------------------------------------------------------|
| All services completed | Send `MarkOrderReadyForReportCommand` → Order moves to `ReadyForReport` |

---

## 6. Aggregate Existence Timeline

| Order Status            | IntakeSession Exists? | ServiceRequests Exist?   | SLA Clocks                                 |
|-------------------------|-----------------------|--------------------------|--------------------------------------------|
| `Created`               | No                    | No                       | Overall (Running)                          |
| `Invited`               | Yes (Invited)         | No                       | Overall, Intake (Running)                  |
| `IntakeInProgress`      | Yes (InProgress)      | No                       | Overall, Intake (Running)                  |
| `IntakeComplete`        | Yes (AwaitingReview)  | No                       | Overall (Running), Intake (Completed)      |
| `ReadyForFulfillment`   | Yes (Submitted)       | Being Created            | Overall, Fulfillment (Running)             |
| `FulfillmentInProgress` | Yes (Submitted)       | Yes (Pending/InProgress) | Overall, Fulfillment (Running)             |
| `ReadyForReport`        | Yes (Submitted)       | Yes (Completed)          | Overall (Running), Fulfillment (Completed) |
| `Closed`                | Yes (Submitted)       | Yes (Completed)          | All (Completed)                            |

---

## 7. Complete Happy Path Flow

```
1.  [User] Creates order via UI
2.  [API] POST /api/subjects → Subject created
3.  [API] POST /api/orders → Order created (Created)
4.  [Handler] OrderStatusChangedSlaHandler starts Overall clock
5.  [API] POST /api/intake/sessions → IntakeSession created (Invited)
6.  [Event] IntakeSessionInvited published
7.  [Handler] IntakeToWorkflowHandler → RecordOrderInviteCommand
8.  [Order] Transitions to Invited
9.  [Handler] OrderStatusChangedSlaHandler starts Intake clock
10. [Subject] Opens intake link with resume token
11. [API] POST /api/intake/sessions/{id}/start
12. [IntakeSession] Transitions to InProgress
13. [Event] IntakeSessionStarted published
14. [Handler] IntakeToWorkflowHandler → MarkOrderIntakeStartedCommand
15. [Order] Transitions to IntakeInProgress
16. [Subject] Completes form and submits
17. [API] POST /api/intake/sessions/{id}/submit
18. [IntakeSession] Transitions to AwaitingReview
19. [Gateway] OrderWorkflowGateway.NotifyIntakeSubmittedAsync()
20. [Order] Transitions to IntakeComplete
21. [Handler] OrderStatusChangedSlaHandler completes Intake clock
22. [System] Auto-accepts or operator accepts
23. [API] POST /api/intake/sessions/{id}/accept
24. [IntakeSession] Transitions to Submitted
25. [Gateway] OrderWorkflowGateway.NotifyIntakeAcceptedAsync()
26. [Order] Transitions to ReadyForFulfillment
27. [Handler] OrderStatusChangedSlaHandler starts Fulfillment clock
28. [Handler] OrderFulfillmentHandler creates ServiceRequests
29. [Handler] OrderFulfillmentHandler → BeginOrderFulfillmentCommand
30. [Order] Transitions to FulfillmentInProgress
31. [Vendors] Process ServiceRequests
32. [API] Vendor callbacks mark services InProgress → Completed
33. [Handler] ServiceCompletionOrderHandler checks completion
34. [Handler] When all complete → MarkOrderReadyForReportCommand
35. [Order] Transitions to ReadyForReport
36. [Handler] OrderStatusChangedSlaHandler completes Fulfillment clock
37. [Operator] Reviews and closes order
38. [Order] Transitions to Closed
39. [Handler] OrderStatusChangedSlaHandler completes Overall clock
```

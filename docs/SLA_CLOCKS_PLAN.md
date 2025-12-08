# SLA Clocks Module Implementation Plan

## Overview

SLA Clocks track time-bound obligations on Orders. They start when an order enters specific states, pause when blocked,
and complete or breach based on deadlines. The module integrates with Workflow via domain events and triggers
Notifications on at-risk/breach conditions.

**Key Design Decisions:**

- SLA targets are **customer-defined** (from service agreements) - defaults are fallbacks only
- Business day calculations from the start (not calendar days)
- 80% at-risk threshold as default
- Separate `Holmes.SlaClocks` module (not embedded in Workflow)

## Architecture Decision: Separate Module

**Decision: New `Holmes.SlaClocks` module**

Reasons:

- SLA logic is complex enough to warrant isolation (business calendar, watchdog service, jurisdiction rules)
- Clean separation of concerns - Workflow owns state transitions, SlaClocks owns time tracking
- Follows existing module pattern (Domain → Application → Infrastructure.Sql)
- Can evolve independently (future: adverse action clocks in Phase 4)

Integration via:

- Subscribe to `OrderStatusChanged` events (no direct Workflow dependency)
- Emit `SlaClockAtRisk`/`SlaClockBreached` events (consumed by Notifications)

## Module Structure

```
src/Modules/SlaClocks/
├── Holmes.SlaClocks.Domain/
│   ├── SlaClock.cs                    # Aggregate root
│   ├── ClockKind.cs                   # Enum: Intake, Fulfillment, Overall, Custom
│   ├── ClockState.cs                  # Enum: Running, AtRisk, Breached, Paused, Completed
│   ├── Events/
│   │   ├── SlaClockStarted.cs
│   │   ├── SlaClockAtRisk.cs
│   │   ├── SlaClockBreached.cs
│   │   ├── SlaClockPaused.cs
│   │   ├── SlaClockResumed.cs
│   │   └── SlaClockCompleted.cs
│   ├── ISlaClockRepository.cs
│   └── ISlaClockUnitOfWork.cs
│
├── Holmes.SlaClocks.Application/
│   ├── Commands/
│   │   ├── StartSlaClockCommand.cs
│   │   ├── PauseSlaClockCommand.cs
│   │   ├── ResumeSlaClockCommand.cs
│   │   ├── MarkClockAtRiskCommand.cs
│   │   ├── MarkClockBreachedCommand.cs
│   │   └── CompleteSlaClockCommand.cs
│   ├── Queries/
│   │   └── GetClocksByOrderQuery.cs
│   ├── EventHandlers/
│   │   └── OrderStatusChangedSlaHandler.cs  # Listens to Workflow events
│   └── Services/
│       └── IBusinessCalendarService.cs      # Interface for deadline calculation
│
├── Holmes.SlaClocks.Application.Abstractions/
│   └── Dtos/
│       └── SlaClockDto.cs
│
└── Holmes.SlaClocks.Infrastructure.Sql/
    ├── SlaClockRepository.cs
    ├── SlaClockUnitOfWork.cs
    ├── SlaClockDbContext.cs
    ├── Entities/
    │   ├── SlaClockDb.cs
    │   ├── BusinessCalendarDb.cs
    │   └── HolidayDb.cs
    ├── Services/
    │   └── BusinessCalendarService.cs
    └── DependencyInjection.cs
```

## Domain Model

### ClockKind Enum

```csharp
public enum ClockKind
{
    Intake = 1,       // Invited → IntakeComplete (default: 1 business day)
    Fulfillment = 2,  // ReadyForRouting → ReadyForReport (default: 3 business days)
    Overall = 3,      // Created → Closed (default: 5 business days)
    Custom = 99       // Future: tenant-defined
}
```

**Terminology Note:** The Order state machine uses `ReadyForRouting`/`RoutingInProgress` states, but the SLA clock uses
`Fulfillment` to better describe what's happening: executing background check services (court searches, verifications,
etc.).

### ClockState Enum

```csharp
public enum ClockState
{
    Running = 1,    // Clock is actively counting
    AtRisk = 2,     // Past 80% threshold, still counting
    Breached = 3,   // Past deadline
    Paused = 4,     // Temporarily stopped (order blocked)
    Completed = 5   // Target reached before deadline
}
```

### SlaClock Aggregate

```csharp
public sealed class SlaClock : AggregateRoot
{
    public UlidId Id { get; private set; }
    public UlidId OrderId { get; private set; }
    public UlidId CustomerId { get; private set; }
    public ClockKind Kind { get; private set; }
    public ClockState State { get; private set; }

    // Time tracking
    public DateTimeOffset StartedAt { get; private set; }
    public DateTimeOffset DeadlineAt { get; private set; }
    public DateTimeOffset? AtRiskThresholdAt { get; private set; }  // When 80% point occurs
    public DateTimeOffset? AtRiskAt { get; private set; }           // When marked at-risk
    public DateTimeOffset? BreachedAt { get; private set; }
    public DateTimeOffset? PausedAt { get; private set; }
    public DateTimeOffset? CompletedAt { get; private set; }
    public string? PauseReason { get; private set; }

    // Pause tracking (accumulated pause time for deadline adjustment)
    public TimeSpan AccumulatedPauseTime { get; private set; }

    // SLA configuration (from customer service agreement)
    public int TargetBusinessDays { get; private set; }
    public decimal AtRiskThresholdPercent { get; private set; }  // e.g., 0.80

    // Methods
    public static SlaClock Start(
        UlidId id,
        UlidId orderId,
        UlidId customerId,
        ClockKind kind,
        DateTimeOffset startedAt,
        DateTimeOffset deadlineAt,
        DateTimeOffset atRiskThresholdAt,
        int targetBusinessDays,
        decimal atRiskThresholdPercent = 0.80m);

    public void Pause(string reason, DateTimeOffset pausedAt);
    public void Resume(DateTimeOffset resumedAt);
    public void MarkAtRisk(DateTimeOffset atRiskAt);
    public void MarkBreached(DateTimeOffset breachedAt);
    public void Complete(DateTimeOffset completedAt);
}
```

### Clock Lifecycle by Order Status

| Order Status Change | Clock Action                            |
|---------------------|-----------------------------------------|
| Created             | Start `Overall` clock                   |
| Invited             | Start `Intake` clock                    |
| IntakeComplete      | Complete `Intake` clock                 |
| ReadyForRouting     | Start `Fulfillment` clock               |
| ReadyForReport      | Complete `Fulfillment` clock            |
| Closed              | Complete `Overall` clock                |
| Blocked             | Pause ALL running clocks for this order |
| ResumeFromBlock     | Resume ALL paused clocks for this order |
| Canceled            | Complete ALL clocks (no breach)         |

### Default SLA Targets

These are fallback defaults when customer service agreement doesn't specify:

| Clock Kind  | Default Target  | At-Risk Threshold |
|-------------|-----------------|-------------------|
| Intake      | 1 business day  | 80% (19.2 hours)  |
| Fulfillment | 3 business days | 80% (2.4 days)    |
| Overall     | 5 business days | 80% (4 days)      |

**Note:** Real SLA targets come from the customer's service agreement. These defaults exist only as fallbacks.

## Business Calendar Service

### Interface

```csharp
public interface IBusinessCalendarService
{
    /// <summary>
    /// Calculate deadline by adding business days to start date.
    /// Excludes weekends and holidays for the given customer/jurisdiction.
    /// </summary>
    DateTimeOffset AddBusinessDays(
        DateTimeOffset start,
        int businessDays,
        UlidId customerId);

    /// <summary>
    /// Calculate when the at-risk threshold occurs.
    /// </summary>
    DateTimeOffset CalculateAtRiskThreshold(
        DateTimeOffset start,
        DateTimeOffset deadline,
        decimal thresholdPercent);

    /// <summary>
    /// Check if a given date is a business day for this customer.
    /// </summary>
    bool IsBusinessDay(DateTimeOffset date, UlidId customerId);
}
```

### Implementation Strategy

Phase 1 (this PR):

- Weekend exclusion (Saturday/Sunday are not business days)
- US Federal holidays (hardcoded list for 2024-2026)
- Customer-specific holidays stored in `holidays` table

Future enhancements:

- Jurisdiction-specific calendars
- Business hours (not just days)
- Timezone-aware calculations

### Database Tables

**Table: `business_calendars`**
| Column | Type | Description |
|--------|------|-------------|
| id | CHAR(26) | ULID primary key |
| customer_id | CHAR(26) | Tenant (nullable for system defaults) |
| name | VARCHAR(128) | Calendar name |
| timezone | VARCHAR(64) | e.g., "America/New_York" |
| is_default | BOOLEAN | Default calendar for customer |

**Table: `holidays`**
| Column | Type | Description |
|--------|------|-------------|
| id | INT | Auto-increment PK |
| calendar_id | CHAR(26) | FK to business_calendars |
| date | DATE | Holiday date |
| name | VARCHAR(128) | Holiday name |
| is_observed | BOOLEAN | Observed vs actual date |

## Event Handler Integration

`OrderStatusChangedSlaHandler` in SlaClocks.Application listens to `OrderStatusChanged`:

```csharp
public sealed class OrderStatusChangedSlaHandler(
    ISlaClockUnitOfWork unitOfWork,
    IBusinessCalendarService calendarService,
    ISender sender
) : INotificationHandler<OrderStatusChanged>
{
    public async Task Handle(OrderStatusChanged notification, CancellationToken ct)
    {
        switch (notification.Status)
        {
            case OrderStatus.Created:
                await StartClockAsync(notification.OrderId, ClockKind.Overall, notification.ChangedAt, ct);
                break;

            case OrderStatus.Invited:
                await StartClockAsync(notification.OrderId, ClockKind.Intake, notification.ChangedAt, ct);
                break;

            case OrderStatus.IntakeComplete:
                await CompleteClockAsync(notification.OrderId, ClockKind.Intake, notification.ChangedAt, ct);
                break;

            case OrderStatus.ReadyForRouting:
                await StartClockAsync(notification.OrderId, ClockKind.Fulfillment, notification.ChangedAt, ct);
                break;

            case OrderStatus.ReadyForReport:
                await CompleteClockAsync(notification.OrderId, ClockKind.Fulfillment, notification.ChangedAt, ct);
                break;

            case OrderStatus.Closed:
                await CompleteClockAsync(notification.OrderId, ClockKind.Overall, notification.ChangedAt, ct);
                break;

            case OrderStatus.Blocked:
                await PauseAllClocksAsync(notification.OrderId, notification.Reason, notification.ChangedAt, ct);
                break;

            case OrderStatus.Canceled:
                await CompleteAllClocksAsync(notification.OrderId, notification.ChangedAt, ct);
                break;

            // ResumeFromBlock handled specially - status returns to previous state
        }
    }
}
```

## Watchdog Background Service

`SlaClockWatchdogService` runs in App.Server:

```csharp
public sealed class SlaClockWatchdogService(
    IServiceScopeFactory scopeFactory,
    ILogger<SlaClockWatchdogService> logger
) : BackgroundService
{
    private static readonly TimeSpan CheckInterval = TimeSpan.FromMinutes(1);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("SlaClockWatchdogService started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CheckClocksAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "SlaClockWatchdogService encountered an error");
            }

            await Task.Delay(CheckInterval, stoppingToken);
        }
    }

    private async Task CheckClocksAsync(CancellationToken ct)
    {
        using var scope = scopeFactory.CreateScope();
        var sender = scope.ServiceProvider.GetRequiredService<ISender>();
        var repository = scope.ServiceProvider.GetRequiredService<ISlaClockRepository>();

        var now = DateTimeOffset.UtcNow;

        // Find running clocks past at-risk threshold (not yet marked)
        var atRiskClocks = await repository.GetRunningClocksPastThresholdAsync(now, ct);
        foreach (var clock in atRiskClocks)
        {
            await sender.Send(new MarkClockAtRiskCommand(clock.Id, now), ct);
        }

        // Find running/at-risk clocks past deadline (not yet breached)
        var breachedClocks = await repository.GetRunningClocksPastDeadlineAsync(now, ct);
        foreach (var clock in breachedClocks)
        {
            await sender.Send(new MarkClockBreachedCommand(clock.Id, now), ct);
        }
    }
}
```

## Notification Integration

SlaClocks emits events → Notifications module subscribes via handler in `App.Integration/NotificationHandlers/`:

| SLA Event          | Notification Trigger                              | Priority |
|--------------------|---------------------------------------------------|----------|
| `SlaClockAtRisk`   | `SlaClockAtRisk` to ops + customer                | High     |
| `SlaClockBreached` | `SlaClockBreached` to ops + customer + compliance | Critical |

## Database Schema

### Table: `sla_clocks`

| Column                    | Type         | Description                 |
|---------------------------|--------------|-----------------------------|
| id                        | CHAR(26)     | ULID primary key            |
| order_id                  | CHAR(26)     | Associated order            |
| customer_id               | CHAR(26)     | Tenant                      |
| kind                      | INT          | ClockKind enum              |
| state                     | INT          | ClockState enum             |
| started_at                | DATETIME(6)  | When clock started          |
| deadline_at               | DATETIME(6)  | Computed deadline           |
| at_risk_threshold_at      | DATETIME(6)  | When 80% point occurs       |
| at_risk_at                | DATETIME(6)  | When marked at-risk         |
| breached_at               | DATETIME(6)  | When breached               |
| paused_at                 | DATETIME(6)  | Current pause start         |
| completed_at              | DATETIME(6)  | When completed              |
| pause_reason              | VARCHAR(256) | Why paused                  |
| accumulated_pause_ms      | BIGINT       | Total pause time in ms      |
| target_business_days      | INT          | SLA target in business days |
| at_risk_threshold_percent | DECIMAL(3,2) | e.g., 0.80                  |

**Indexes:**

- `(order_id)` - find clocks for an order
- `(state, at_risk_threshold_at)` - watchdog at-risk queries
- `(state, deadline_at)` - watchdog breach queries
- `(customer_id, state)` - tenant dashboard

## Implementation Order

1. **Domain Layer** - SlaClock aggregate, enums, events, repository interface
2. **Application Layer** - Commands, queries, OrderStatusChangedSlaHandler
3. **Application.Abstractions** - DTOs
4. **Infrastructure Layer** - DbContext, repository, business calendar service
5. **Watchdog Service** - Background service in App.Server
6. **Notification Integration** - Handler in App.Integration
7. **DI Wiring** - Add to ef-reset.ps1, wire up in App.Server

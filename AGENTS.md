## Target Architecture Rules

These rules describe the intended .NET 9 **Modular Monolith** shape going forward. Each bounded context is a
self-contained module under `src/Modules/{ModuleName}/` with these projects:

### For each module, create five projects:

1. **`Holmes.{Module}.Domain`** - The core domain layer:
    - Aggregate roots (e.g., `Customer.cs`, `SlaClock.cs`)
    - Value objects under `ValueObjects/`
    - Domain events under `Events/` (e.g., `CustomerRegistered.cs`)
    - Repository interfaces (`I{Entity}Repository.cs`)
    - Unit of Work interface (`I{Module}UnitOfWork.cs`)
    - Enums for entity states

2. **`Holmes.{Module}.Application`** - Application/use case layer:
    - Commands under `Commands/` (CQRS command handlers)
    - Queries under `Queries/` (CQRS query handlers)
    - Event handlers under `EventHandlers/` for domain event projections and side effects

3. **`Holmes.{Module}.Application.Abstractions`** - Contracts for the application layer:
    - DTOs under `Dtos/`
    - Query interfaces under `Queries/` (e.g., `I{Entity}Queries.cs`)
    - Projection writer interfaces under `Projections/`
    - Service interfaces under `Services/`
    - Notification/broadcaster interfaces under `Notifications/`

4. **`Holmes.{Module}.Infrastructure.Sql`** - EF Core persistence:
    - `{Module}DbContext.cs`
    - `{Module}UnitOfWork.cs`
    - Database entities under `Entities/` (e.g., `CustomerDb.cs`)
    - Repositories under `Repositories/` implementing domain interfaces
    - Query implementations under `Queries/` (implementing interfaces from Application.Abstractions)
    - Projections under `Projections/`
    - Mappers under `Mappers/`
    - Specifications under `Specifications/`
    - EF Migrations under `Migrations/`
    - `DependencyInjection.cs` for service registration

5. **`Holmes.{Module}.Tests`** - Unit tests for the module

### Dependency graph

```
Application ──────► Application.Abstractions ◄────── Infrastructure.Sql
     │                        │                              │
     └────────► Domain ◄──────┴──────────────────────────────┘
```

- **Domain** has no dependencies (pure domain logic)
- **Application.Abstractions** depends on Domain (for value objects in DTOs)
- **Application** depends on Domain and Application.Abstractions
- **Infrastructure.Sql** depends on Domain and Application.Abstractions (NOT Application)

### Cross-module references

When ModuleA needs types from ModuleB:

- **Allowed:** `ModuleA.Application` → `ModuleB.Application.Abstractions`
- **Allowed:** `ModuleA.Infrastructure.Sql` → `ModuleB.Application.Abstractions`
- **Forbidden:** Direct references to another module's Domain, Application, or Infrastructure.Sql

### Cross-module event handlers

When a domain event from Module A needs to trigger behavior in Module B, the handler belongs in
the consuming module's Application and depends only on ModuleA.Application.Abstractions.

### App projects

- **`Holmes.App.Server`** - API surface, background services, and integration wiring.
- **`Holmes.App.Infrastructure.Security`** - Host security/identity wiring and policies.

### Key patterns

- CQRS (Command Query Responsibility Segregation)
- Domain Events for cross-module communication
- Event-driven projections for read models
- Unit of Work pattern per module
- Separate database entities (`*Db.cs`) from domain entities
- Abstractions layer to prevent circular dependencies
- No cross-module transactions; use outbox + integration events between modules

## Controller and Transaction Rules (Non-Negotiable)

Core Rules (Must Always Hold)

- One write endpoint → one use-case command → one transaction
- Any HTTP endpoint that is not GET must call IMediator.Send() exactly once.
- Controllers must not orchestrate multiple commands or queries.
- Prerequisites use synchronous Gateways (Contracts)
- If a use case cannot proceed without something existing, the command handler may call a Gateway interface in a *
  .Contracts project.
- Example: ISubjectGateway.EnsureSubjectAsync(...)
- Gateways are only for prerequisites, not side effects.
- Reactions use integration events (outbox-backed)
- Anything that happens because a use case succeeded (intake session, notifications, SLA clocks, projections) must be
  triggered by integration events, not direct calls.
- No "EnsureIntakeSession", no chained commands.
- No IMediator.Send() inside command handlers
- Command handlers must not compose other commands.
- They may:
    - Call domain methods
    - Call gateways
    - Save once
    - Emit integration events
- Queries are excluded from this rule.
- Controllers are thin
- Validate request shape
- Map to a single command
- Send command
- Return result

Enforcement (Must Be Implemented First)

- Test 1 — Controllers
    - Any method with [HttpPost], [HttpPut], [HttpDelete], or [HttpPatch]
    - Must contain exactly one call to mediator.Send(...)
    - Fail otherwise
- Test 2 — Command Handlers
    - Any *CommandHandler may not contain .Send(
    - Fail build if violated

## Order Workflow Prompt (Non-Negotiable)

Implement an event-driven workflow where Orders owns the workflow state with no cross-module transactions. The flow is:

1) Orders: CreateOrder creates a workflow order with SubjectEmail (+ optional Phone for OTP). SubjectId and
   ActiveIntakeSessionId are null. Orders publishes OrderRequested.
2) Subjects: consumes OrderRequested, resolves/creates Subject, publishes SubjectResolved (OrderId + SubjectId).
3) Orders: consumes SubjectResolved, sets SubjectId, publishes OrderSubjectAssigned.
4) IntakeSessions: consumes OrderSubjectAssigned, creates intake session + invite/OTP initiation, publishes
   IntakeSessionStarted (OrderId + IntakeSessionId).
5) IntakeSessions later publishes IntakeSubmitted (OrderId + IntakeSessionId). Orders consumes it to advance status.

Critical constraints:

- Integration event names must be module-local and must NOT mention other modules.
  Allowed: OrderRequested, OrderSubjectAssigned, SubjectResolved, IntakeSessionStarted, IntakeSubmitted
- No shared transactions; each module commits independently.
- Use outbox/deferred dispatch (SaveChangesAsync(true)) for integration events.
- Controllers: non-GET endpoints call IMediator.Send exactly once.
- Command handlers must not call IMediator.Send.
- Handlers must be idempotent and safe under retries.

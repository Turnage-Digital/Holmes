# Holmes Architecture (Target State)

This document captures the intended target architecture after the upcoming large-scale changes. It is forward-looking
and may not match the current repository in every detail.

---

## Conventions

- Do not include "Generated with Claude Code" or Co-Authored-By lines in commits or PRs.

---

## 1) Executive Summary

Holmes is a modular, event-driven platform for background screening intake, fulfillment, and compliance workflows.
The system is designed to keep bounded contexts isolated, enable independent evolution, and support near-real-time
read models for internal and external clients.

---

## 2) Module Architecture (Target)

Holmes is a .NET 9 **Modular Monolith** using **Clean Architecture** and **Domain-Driven Design**. Each bounded
context is a self-contained module under `src/Modules/{ModuleName}/` with five projects:

1. `Holmes.{Module}.Domain`
    - Aggregate roots (e.g., `Customer.cs`, `SlaClock.cs`)
    - Value objects under `ValueObjects/`
    - Domain events under `Events/`
    - Repository interfaces (`I{Entity}Repository.cs`)
    - Unit of Work interface (`I{Module}UnitOfWork.cs`)
    - Enums for entity states

2. `Holmes.{Module}.Application`
    - Commands under `Commands/` (CQRS command handlers)
    - Queries under `Queries/` (CQRS query handlers)
    - Event handlers under `EventHandlers/` for domain event projections and side effects

3. `Holmes.{Module}.Application.Abstractions`
    - DTOs under `Dtos/`
    - Query interfaces under `Queries/` (e.g., `I{Entity}Queries.cs`)
    - Projection writer interfaces under `Projections/`
    - Service interfaces under `Services/`
    - Notification/broadcaster interfaces under `Notifications/`

4. `Holmes.{Module}.Infrastructure.Sql`
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

5. `Holmes.{Module}.Tests`
    - Unit tests for the module

Dependency graph:

```
Application ──────► Application.Abstractions ◄────── Infrastructure.Sql
     │                        │                              │
     └────────► Domain ◄──────┴──────────────────────────────┘
```

- Domain has no dependencies (pure domain logic)
- Application.Abstractions depends on Domain (for value objects in DTOs)
- Application depends on Domain and Application.Abstractions
- Infrastructure.Sql depends on Domain and Application.Abstractions (NOT Application)

---

## 3) Cross-Module Integration

- Allowed references: `ModuleA.Application` → `ModuleB.Application.Abstractions` and
  `ModuleA.Infrastructure.Sql` → `ModuleB.Application.Abstractions`.
- Forbidden references: direct dependencies on another module's Domain, Application, or Infrastructure.Sql.
- Cross-module integration handlers live in the consuming module's Application and only reference the
  producer's Application.Abstractions.
- Integration event contracts live in the producer's `Application.Abstractions/IntegrationEvents/`.
- Integration event publishers and consumers live in each module's `Application/EventHandlers/`.
- No cross-module transactions; use the outbox + integration events between modules.
- See `docs/INTEGRATION_INDEX.md` for the current producer/consumer map.

---

## 4) Runtime Hosts and App Projects

- `Holmes.App.Server`
    - API surface, background services, and integration wiring.
- `Holmes.App.Infrastructure.Security`
    - Host security/identity wiring and policies.

There is no central app application layer; integration handlers live in modules.

---

## 5) Eventing, Unit of Work, and Outbox

- Aggregates emit domain events and are persisted with their `EventRecord` in one transaction.
- Domain events are dispatched after commit via MediatR.
- Deferred dispatch / outbox processing is the mechanism for cross-module and external integration events.

---

## 6) Projections (CQRS Read Models)

- Read models are updated by event handlers inside each module.
- Projection writers live in each module's Infrastructure project, and are invoked by the host.
- Query interfaces live in Application.Abstractions and are implemented in Infrastructure.Sql.

---

## 7) Interface Patterns

- APIs expose command and query endpoints, backed by the module boundaries above.
- Live update delivery is supported through event-driven read models and a streaming mechanism (SSE or equivalent).

---

## 8) Bounded Contexts

Module boundaries are defined by business capabilities, not technical layers. The module set will evolve as the
platform expands; new capabilities should be introduced as separate modules using the standard architecture above.

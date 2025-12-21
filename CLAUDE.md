# Claude Code Instructions

Do not include "Generated with Claude Code" or Co-Authored-By lines in commits or PRs.

## Module Architecture

This is a .NET 9 **Modular Monolith** following **Clean Architecture** and **Domain-Driven Design** principles. Each bounded context is a self-contained module under `src/Modules/{ModuleName}/` with these projects:

### For each module, create 5 projects:

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

### Dependency graph:

```
Application ──────► Application.Abstractions ◄────── Infrastructure.Sql
     │                        │                              │
     └────────► Domain ◄──────┴──────────────────────────────┘
```

- **Domain** has no dependencies (pure domain logic)
- **Application.Abstractions** depends on Domain (for value objects in DTOs)
- **Application** depends on Domain and Application.Abstractions
- **Infrastructure.Sql** depends on Domain and Application.Abstractions (NOT Application)

### Cross-module references:

When ModuleA needs types from ModuleB:
- **Allowed:** `ModuleA.Application` → `ModuleB.Application.Abstractions`
- **Allowed:** `ModuleA.Infrastructure.Sql` → `ModuleB.Application.Abstractions`
- **Forbidden:** Direct references to another module's Domain, Application, or Infrastructure.Sql

### Cross-module event handlers:

When a domain event from Module A needs to trigger behavior in Module B, the handler belongs in `Holmes.App.Integration`, NOT in either module's Application project.

### Key patterns:
- CQRS (Command Query Responsibility Segregation)
- Domain Events for cross-module communication
- Event-driven projections for read models
- Unit of Work pattern per module
- Separate database entities (`*Db.cs`) from domain entities
- Abstractions layer to prevent circular dependencies

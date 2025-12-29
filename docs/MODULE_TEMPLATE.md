# Module Template & Conventions

Holmes modules follow the same three-project pattern so new slices behave the
same way as the existing Users/Customers/Subjects work. Use this as a checklist
whenever a new bounded context is created.

## Project Layout

```
src/Modules/<Feature>/
  Holmes.<Feature>.Domain/
    <feature>.csproj
    - Aggregates/Entities/ValueObjects
    - Domain events and repository interfaces (write-focused)
    - I<Feature>UnitOfWork interface

  Holmes.<Feature>.Application.Abstractions/
    <feature>.Application.Abstractions.csproj
    - DTOs for queries and cross-module contracts
    - Query interfaces (I<Feature>Queries)
    - Broadcaster/notification interfaces
    - Integration event contracts

  Holmes.<Feature>.Application/
    <feature>.Application.csproj
    - Commands and command handlers (write side)
    - Query handlers (delegate to I<Feature>Queries)
    - MediatR pipeline behaviors

  Holmes.<Feature>.Infrastructure.Sql/
    <feature>.Infrastructure.Sql.csproj
    - DbContext + EF mappings
    - Repository implementations (write-focused)
    - Query implementations (Sql<Feature>Queries)
    - Specifications for query logic
    - DependencyInjection.cs (`Add<Feature>InfrastructureSql`)
```

> If a feature needs non-SQL infrastructure (e.g., caching, search) create
> sibling projects named `Infrastructure.<Provider>` but keep the SQL project
> as the anchor for data access.

## Project References

| Project                                     | References                                                                                               |
|---------------------------------------------|----------------------------------------------------------------------------------------------------------|
| `Holmes.<Feature>.Domain`                   | `Holmes.Core.Domain` only                                                                                |
| `Holmes.<Feature>.Application.Abstractions` | `Holmes.<Feature>.Domain`, `Holmes.Core.Domain`                                                          |
| `Holmes.<Feature>.Application`              | `Holmes.<Feature>.Domain`, `Holmes.<Feature>.Application.Abstractions`, `Holmes.Core.Application`        |
| `Holmes.<Feature>.Infrastructure.Sql`       | `Holmes.<Feature>.Domain`, `Holmes.<Feature>.Application.Abstractions`, `Holmes.Core.Infrastructure.Sql` |

**Critical**: Infrastructure projects reference `Application.Abstractions` (for query interfaces and DTOs),
but NEVER reference the `Application` project directly. This enables database swappability.

`Holmes.App.Server` references the Application + Infrastructure projects only.
`Holmes.App.Infrastructure` references only `Application.Abstractions` projects (never Infrastructure.Sql).
Tests reference the Application layer (for handlers) plus Infrastructure for integration scenarios.

## Cross-Module Boundaries (CRITICAL)

**A module MUST NEVER directly reference another module's Domain or Application projects.**

This is a fundamental DDD rule that preserves bounded context independence:

### ❌ FORBIDDEN References

```
Holmes.SlaClocks.Application → Holmes.Orders.Domain        # NO!
Holmes.Services.Application  → Holmes.Subjects.Domain        # NO!
Holmes.IntakeSessions.Infrastructure → Holmes.Orders.Application   # NO!
```

### ✅ ALLOWED Cross-Module Communication

When Module A needs data or behavior from Module B:

1. **Module B exposes an `*.Application.Abstractions` project** containing:
    - DTOs (data transfer objects)
    - Query interfaces (e.g., `IOrderQueries`, `IUserQueries`)
    - Event contracts for integration events
    - Broadcaster/notification interfaces

2. **Module A references only `Holmes.ModuleB.Application.Abstractions`** — never
   the Domain or Application implementation.

3. **The host wires up the implementations** via DI, keeping modules decoupled.

4. **Controllers and middleware** inject query interfaces (e.g., `IUserQueries`), never DbContexts.

### Example: SlaClocks needs Order status

```
# WRONG - direct domain reference
Holmes.SlaClocks.Application → Holmes.Orders.Domain

# RIGHT - abstraction reference
Holmes.Orders.Application.Abstractions/
  └── IOrderStatusProvider.cs
  └── Dtos/OrderStatusDto.cs

Holmes.SlaClocks.Application → Holmes.Orders.Application.Abstractions
Holmes.Orders.Infrastructure.Sql implements IOrderStatusProvider
Host registers IOrderStatusProvider in DI
```

### Why This Matters

- **Testability**: Modules can be tested in isolation with mocks
- **Deployability**: Modules could theoretically be deployed independently
- **Evolvability**: Internal domain changes don't cascade across modules
- **Clarity**: Dependencies are explicit contracts, not hidden couplings

### Cross-Module Integration Handlers

When a module needs to react to another module, the handler lives in the consuming module's Application and depends
on the producer's `*.Application.Abstractions` integration events only.

```
# Example: OrderStatusChanged (Workflow) → Start SLA clocks (SlaClocks)

Holmes.SlaClocks.Application/
  └── EventHandlers/
      └── OrderStatusChangedSlaHandler.cs  # Handles Orders integration event, sends SlaClocks commands

Holmes.SlaClocks.Application.csproj references:
  - Holmes.Orders.Application.Abstractions (for OrderStatusChangedIntegrationEvent)
```

This keeps module boundaries clean while allowing event-driven integration without direct cross-module Application
references.

### Integration Event Flow (Standard)

- Contracts live in `...Application.Abstractions/IntegrationEvents/`.
- Producers publish integration events from domain events via `...Application/EventHandlers/*IntegrationEventPublisher.cs`.
- Consumers handle integration events in `...Application/EventHandlers/` and translate to local commands.
- Use `SaveChangesAsync(true)` when you need outbox delivery; the `DeferredDispatchProcessor` publishes after commit.
- Keep integration handlers thin; push composition into commands or services to avoid "gore" handlers.

## CQRS Pattern

Holmes uses Command Query Responsibility Segregation (CQRS) to separate read and write concerns:

### Write Side (Domain + Repository)

- **Repository interfaces** in Domain are write-focused: `GetByIdAsync`, `Add`, `Update`, `Delete`
- **Commands** mutate state through the UnitOfWork and repositories
- Repositories return domain entities for mutation

### Read Side (Queries)

- **Query interfaces** (`I<Feature>Queries`) live in `Application.Abstractions`
- **Query implementations** (`Sql<Feature>Queries`) live in `Infrastructure.Sql`
- Queries return **DTOs only** — never domain entities
- Use **Specifications** for reusable query logic

### Example Query Interface

```csharp
// In Application.Abstractions/Queries/IUserQueries.cs
public interface IUserQueries
{
    Task<UserDto?> GetByIdAsync(string userId, CancellationToken ct);
    Task<UserDto?> GetByEmailAsync(string email, CancellationToken ct);
    Task<IReadOnlyList<UserDto>> GetAllAsync(CancellationToken ct);
}
```

### Example Query Implementation

```csharp
// In Infrastructure.Sql/Queries/SqlUserQueries.cs
public sealed class SqlUserQueries(UsersDbContext dbContext) : IUserQueries
{
    public async Task<UserDto?> GetByIdAsync(string userId, CancellationToken ct)
    {
        var spec = new UserByIdSpec(userId);
        return await dbContext.Users
            .AsNoTracking()
            .ApplySpecification(spec)
            .Select(u => new UserDto(u.Id, u.Email, u.DisplayName))
            .FirstOrDefaultAsync(ct);
    }
}
```

### Why CQRS?

- **Database swappability**: Swap `Infrastructure.Sql` for `Infrastructure.MsSql` without touching Application
- **Performance**: Read models can be optimized independently (no tracking, projections)
- **Testability**: Query interfaces can be mocked in tests
- **Security**: Controllers never see DbContext or domain internals

## Unit of Work Pattern

Each feature exposes an interface in Domain:

```csharp
public interface I<Feature>UnitOfWork : IUnitOfWork
{
    I<Feature>Repository <Feature> { get; }
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
```

The Infrastructure project implements the interface and wires it up in
`DependencyInjection.Add<Feature>InfrastructureSql`. Always register the
UnitOfWork as scoped so it can collect domain events per request.

## Dependency Injection

Every Infrastructure project contains a single entry point:

```csharp
public static class DependencyInjection
{
    public static IServiceCollection Add<Feature>InfrastructureSql(
        this IServiceCollection services,
        string connectionString,
        ServerVersion version)
    {
        services.AddDbContext<<Feature>DbContext>(options =>
            options.UseMySql(connectionString, version));

        services.AddScoped<I<Feature>UnitOfWork, <Feature>UnitOfWork>();

        return services;
    }
}
```

This keeps composition centralized inside `Holmes.App.Server/HostingExtensions`
and guarantees every module exports the same surface area. Repositories are not registered directly; application code
must request the unit of work and access repositories via its properties to honor the transaction boundary.

## Scaffolding Steps

1. Copy the folder skeleton above into `src/Modules/<Feature>/`.
2. Create project files by cloning an existing module and updating:
    - `<RootNamespace>` and `<AssemblyName>`
    - `ProjectReference` blocks following the table above
3. Add `I<Feature>UnitOfWork` to Domain and a `DependencyInjection` helper to
   Infrastructure (see template snippet).
4. Register the module in `HostingExtensions.AddInfrastructure` and add its
   Application assembly to the MediatR registration list.
5. Add a minimal integration test that instantiates the UnitOfWork and exercises
   domain events (see `Holmes.Core.Tests/UnitOfWorkDomainEventsTests.cs`).
6. When adding additional DbContexts (e.g., Identity/IdentityServer) in the same
   assembly, give each migration its own output directory and, if necessary, its own
   startup project in `ef-reset.ps1` so resets can drop/recreate the database once and
   rebuild every context’s “Initial” migration cleanly.

Following this template keeps module wiring consistent and prevents accidental
cross-layer references.

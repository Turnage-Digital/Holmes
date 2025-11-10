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
    - Domain events and interfaces (e.g., I<Feature>UnitOfWork)

  Holmes.<Feature>.Application/
    <feature>.Application.csproj
    - Commands, queries, validators
    - MediatR handlers, pipeline behaviors
    - DTOs specific to the feature

  Holmes.<Feature>.Infrastructure.Sql/
    <feature>.Infrastructure.Sql.csproj
    - DbContext + EF mappings
    - Repository/UnitOfWork implementations
    - DependencyInjection.cs (`Add<Feature>InfrastructureSql`)
```

> If a feature needs non-SQL infrastructure (e.g., caching, search) create
> sibling projects named `Infrastructure.<Provider>` but keep the SQL project
> as the anchor for data access.

## Project References

| Project                             | References                                                                              |
|-------------------------------------|-----------------------------------------------------------------------------------------|
| `Holmes.<Feature>.Domain`           | `Holmes.Core.Domain` only.                                                              |
| `Holmes.<Feature>.Application`      | `Holmes.<Feature>.Domain`, `Holmes.Core.Application`                                    |
| `Holmes.<Feature>.Infrastructure.*` | `Holmes.<Feature>.Domain`, `Holmes.Core.Infrastructure.*` (never reference Application) |

`Holmes.App.Server` references the Application + Infrastructure projects only.
Tests reference the Application layer (for handlers) plus Infrastructure for
integration scenarios.

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

Following this template keeps module wiring consistent and prevents accidental
cross-layer references.

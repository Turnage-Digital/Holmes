# Unit of Work & Domain Events

Aggregates that produce MediatR notifications implement `IHasDomainEvents`, exposing a read-only list of pending events plus `ClearDomainEvents()`. Repository methods must call their module's UnitOfWork (e.g., `UsersUnitOfWork`) to collect each aggregate before returning. The shared `UnitOfWork<TContext>` stores those notifications, executes `SaveChangesAsync`, and only when the transaction succeeds does it publish the captured events via MediatR. This guarantees that every domain event is emitted once per committed transaction without a separate queue.

**Usage**

1. Aggregate mutates state and records events internally.
2. Repository persists the aggregate and calls `_unitOfWork.RegisterDomainEvents(aggregate)`.
3. When the command handler finishes, `*UnitOfWork.SaveChangesAsync()` commits the DbContext and publishes the collected domain events.
4. `ClearDomainEvents()` is called automatically, so aggregates are ready for subsequent mutations.

When a command touches multiple aggregates, call `RegisterDomainEvents` for each one before exiting the repository so that all notifications participate in the same transaction boundary.

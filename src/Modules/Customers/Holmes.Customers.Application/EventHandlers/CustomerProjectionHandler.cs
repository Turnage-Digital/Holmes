using Holmes.Customers.Application.Abstractions.Projections;
using Holmes.Customers.Domain;
using Holmes.Customers.Domain.Events;
using MediatR;

namespace Holmes.Customers.Application.EventHandlers;

/// <summary>
/// Handles customer domain events to maintain the customer projection table.
/// This replaces the synchronous UpsertDirectory calls in the repository.
/// </summary>
public sealed class CustomerProjectionHandler(ICustomerProjectionWriter writer)
    : INotificationHandler<CustomerRegistered>,
      INotificationHandler<CustomerRenamed>,
      INotificationHandler<CustomerSuspended>,
      INotificationHandler<CustomerReactivated>,
      INotificationHandler<CustomerAdminAssigned>,
      INotificationHandler<CustomerAdminRemoved>
{
    public Task Handle(CustomerRegistered notification, CancellationToken cancellationToken)
    {
        var model = new CustomerProjectionModel(
            notification.CustomerId.ToString(),
            notification.Name,
            CustomerStatus.Active,
            notification.RegisteredAt,
            AdminCount: 0);

        return writer.UpsertAsync(model, cancellationToken);
    }

    public Task Handle(CustomerRenamed notification, CancellationToken cancellationToken)
    {
        var model = new CustomerProjectionModel(
            notification.CustomerId.ToString(),
            notification.Name,
            CustomerStatus.Active,
            notification.RenamedAt,
            AdminCount: 0);

        return writer.UpsertAsync(model, cancellationToken);
    }

    public Task Handle(CustomerSuspended notification, CancellationToken cancellationToken)
    {
        return writer.UpdateStatusAsync(
            notification.CustomerId.ToString(),
            CustomerStatus.Suspended,
            cancellationToken);
    }

    public Task Handle(CustomerReactivated notification, CancellationToken cancellationToken)
    {
        return writer.UpdateStatusAsync(
            notification.CustomerId.ToString(),
            CustomerStatus.Active,
            cancellationToken);
    }

    public Task Handle(CustomerAdminAssigned notification, CancellationToken cancellationToken)
    {
        return writer.UpdateAdminCountAsync(
            notification.CustomerId.ToString(),
            delta: 1,
            cancellationToken);
    }

    public Task Handle(CustomerAdminRemoved notification, CancellationToken cancellationToken)
    {
        return writer.UpdateAdminCountAsync(
            notification.CustomerId.ToString(),
            delta: -1,
            cancellationToken);
    }
}

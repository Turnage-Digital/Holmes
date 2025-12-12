using System.Collections.ObjectModel;
using Holmes.Core.Domain;
using Holmes.Core.Domain.ValueObjects;
using Holmes.Customers.Domain.Events;
using Holmes.Customers.Domain.ValueObjects;
using MediatR;

namespace Holmes.Customers.Domain;

public sealed class Customer : AggregateRoot
{
    private readonly List<CustomerAdmin> _admins = [];

    private Customer()
    {
    }

    public UlidId Id { get; private set; }

    public string Name { get; private set; } = null!;

    public CustomerStatus Status { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public IReadOnlyCollection<CustomerAdmin> Admins => new ReadOnlyCollection<CustomerAdmin>(_admins);

    public static Customer Register(UlidId id, string name, DateTimeOffset registeredAt)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        var customer = new Customer();
        customer.Apply(new CustomerRegistered(id, name, registeredAt));
        return customer;
    }

    public static Customer Rehydrate(
        UlidId id,
        string name,
        CustomerStatus status,
        DateTimeOffset createdAt,
        IEnumerable<CustomerAdmin> admins
    )
    {
        var customer = new Customer
        {
            Id = id,
            Name = name,
            Status = status,
            CreatedAt = createdAt
        };

        customer._admins.AddRange(admins);
        return customer;
    }

    public void Rename(string name, DateTimeOffset timestamp)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        if (string.Equals(name, Name, StringComparison.Ordinal))
        {
            return;
        }

        Emit(new CustomerRenamed(Id, name, timestamp));
        Name = name;
    }

    public void AssignAdmin(UlidId userId, UlidId assignedBy, DateTimeOffset timestamp)
    {
        if (_admins.Any(a => a.UserId == userId))
        {
            return;
        }

        _admins.Add(new CustomerAdmin(userId, assignedBy, timestamp));
        Emit(new CustomerAdminAssigned(Id, userId, assignedBy, timestamp));
    }

    public void RemoveAdmin(UlidId userId, UlidId removedBy, DateTimeOffset timestamp)
    {
        var existing = _admins.FirstOrDefault(a => a.UserId == userId);
        if (existing is null)
        {
            return;
        }

        if (_admins.Count == 1)
        {
            throw new InvalidOperationException("Cannot remove the last admin from a customer.");
        }

        _admins.Remove(existing);
        Emit(new CustomerAdminRemoved(Id, userId, removedBy, timestamp));
    }

    public void Suspend(string reason, UlidId performedBy, DateTimeOffset timestamp)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(reason);

        if (Status == CustomerStatus.Suspended)
        {
            return;
        }

        Emit(new CustomerSuspended(Id, reason, performedBy, timestamp));
        Status = CustomerStatus.Suspended;
    }

    public void Reactivate(UlidId performedBy, DateTimeOffset timestamp)
    {
        if (Status == CustomerStatus.Active)
        {
            return;
        }

        Emit(new CustomerReactivated(Id, performedBy, timestamp));
        Status = CustomerStatus.Active;
    }

    private void Apply(CustomerRegistered @event)
    {
        Id = @event.CustomerId;
        Name = @event.Name;
        Status = CustomerStatus.Active;
        CreatedAt = @event.RegisteredAt;
        Emit(@event);
    }

    private void Emit(INotification @event)
    {
        AddDomainEvent(@event);
    }

    public override string GetStreamId()
    {
        return $"{GetStreamType()}:{Id}";
    }

    public override string GetStreamType()
    {
        return "Customer";
    }
}
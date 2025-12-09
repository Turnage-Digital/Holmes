using Holmes.Core.Domain.ValueObjects;
using Holmes.Customers.Application.EventHandlers;
using Holmes.Customers.Domain;
using Holmes.Customers.Domain.Events;
using Holmes.Customers.Infrastructure.Sql;
using Holmes.Customers.Infrastructure.Sql.Projections;
using Holmes.Customers.Infrastructure.Sql.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Holmes.Customers.Tests;

[TestFixture]
public class CustomerTests
{
    [Test]
    public void Register_Creates_Active_Customer()
    {
        var customer = Customer.Register(UlidId.NewUlid(), "Acme", DateTimeOffset.UtcNow);

        Assert.Multiple(() =>
        {
            Assert.That(customer.Status, Is.EqualTo(CustomerStatus.Active));
            Assert.That(customer.Name, Is.EqualTo("Acme"));
        });
    }

    [Test]
    public void RemoveAdmin_Prevents_Removing_Last_Admin()
    {
        var id = UlidId.NewUlid();
        var customer = Customer.Register(id, "Acme", DateTimeOffset.UtcNow);
        var admin = UlidId.NewUlid();
        customer.AssignAdmin(admin, admin, DateTimeOffset.UtcNow);

        Assert.That(() => customer.RemoveAdmin(admin, admin, DateTimeOffset.UtcNow), Throws.InvalidOperationException);
    }

    [Test]
    public async Task CustomerProjectionHandler_CustomerRegistered_WritesProjection()
    {
        await using var context = CreateCustomersDbContext();
        var writer = new SqlCustomerProjectionWriter(context);
        var handler = new CustomerProjectionHandler(writer);
        var customerId = UlidId.NewUlid();

        var @event = new CustomerRegistered(customerId, "Acme Corp", DateTimeOffset.UtcNow);
        await handler.Handle(@event, CancellationToken.None);

        var projection = await context.CustomerProjections.SingleAsync();
        Assert.Multiple(() =>
        {
            Assert.That(projection.CustomerId, Is.EqualTo(customerId.ToString()));
            Assert.That(projection.Name, Is.EqualTo("Acme Corp"));
            Assert.That(projection.Status, Is.EqualTo(CustomerStatus.Active));
            Assert.That(projection.AdminCount, Is.EqualTo(0));
        });
    }

    [Test]
    public async Task CustomerProjectionHandler_CustomerRenamed_UpdatesProjection()
    {
        await using var context = CreateCustomersDbContext();
        var writer = new SqlCustomerProjectionWriter(context);
        var handler = new CustomerProjectionHandler(writer);
        var customerId = UlidId.NewUlid();

        // First create the projection
        var registered = new CustomerRegistered(customerId, "Acme Corp", DateTimeOffset.UtcNow);
        await handler.Handle(registered, CancellationToken.None);

        // Then rename
        var renamed = new CustomerRenamed(customerId, "Acme Holdings", DateTimeOffset.UtcNow);
        await handler.Handle(renamed, CancellationToken.None);

        var projection = await context.CustomerProjections.SingleAsync();
        Assert.That(projection.Name, Is.EqualTo("Acme Holdings"));
    }

    [Test]
    public async Task CustomerProjectionHandler_AdminAssigned_UpdatesAdminCount()
    {
        await using var context = CreateCustomersDbContext();
        var writer = new SqlCustomerProjectionWriter(context);
        var handler = new CustomerProjectionHandler(writer);
        var customerId = UlidId.NewUlid();

        // Create the projection
        var registered = new CustomerRegistered(customerId, "Acme Corp", DateTimeOffset.UtcNow);
        await handler.Handle(registered, CancellationToken.None);

        // Assign two admins
        var admin1 = UlidId.NewUlid();
        var admin2 = UlidId.NewUlid();
        await handler.Handle(new CustomerAdminAssigned(customerId, admin1, admin1, DateTimeOffset.UtcNow), CancellationToken.None);
        await handler.Handle(new CustomerAdminAssigned(customerId, admin2, admin2, DateTimeOffset.UtcNow), CancellationToken.None);

        var projection = await context.CustomerProjections.SingleAsync();
        Assert.That(projection.AdminCount, Is.EqualTo(2));

        // Remove one admin
        await handler.Handle(new CustomerAdminRemoved(customerId, admin1, admin2, DateTimeOffset.UtcNow), CancellationToken.None);

        projection = await context.CustomerProjections.SingleAsync();
        Assert.That(projection.AdminCount, Is.EqualTo(1));
    }

    [Test]
    public async Task SqlRepository_GetByIdAsync_RehydratesAggregate()
    {
        await using var context = CreateCustomersDbContext();
        var repository = new SqlCustomerRepository(context);
        var customer = Customer.Register(UlidId.NewUlid(), "Acme Corp", DateTimeOffset.UtcNow);
        var admin = UlidId.NewUlid();
        customer.AssignAdmin(admin, admin, DateTimeOffset.UtcNow);

        await repository.AddAsync(customer, CancellationToken.None);
        await context.SaveChangesAsync(CancellationToken.None);

        var fetched = await repository.GetByIdAsync(customer.Id, CancellationToken.None);
        Assert.That(fetched, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(fetched!.Name, Is.EqualTo("Acme Corp"));
            Assert.That(fetched.Admins.Select(a => a.UserId), Does.Contain(admin));
        });
    }

    private static CustomersDbContext CreateCustomersDbContext()
    {
        var options = new DbContextOptionsBuilder<CustomersDbContext>()
            .UseInMemoryDatabase($"customers-tests-{Guid.NewGuid()}")
            .Options;
        return new CustomersDbContext(options);
    }
}
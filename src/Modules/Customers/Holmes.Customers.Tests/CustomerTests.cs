using Holmes.Core.Domain.ValueObjects;
using Holmes.Customers.Domain;
using Holmes.Customers.Infrastructure.Sql;
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
    public async Task SqlRepository_AddAsync_WritesDirectoryEntry()
    {
        await using var context = CreateCustomersDbContext();
        var repository = new SqlCustomerRepository(context);
        var customer = Customer.Register(UlidId.NewUlid(), "Acme Corp", DateTimeOffset.UtcNow);
        var admin = UlidId.NewUlid();
        customer.AssignAdmin(admin, admin, DateTimeOffset.UtcNow);

        await repository.AddAsync(customer, CancellationToken.None);
        await context.SaveChangesAsync(CancellationToken.None);

        var directory = await context.CustomerDirectory.SingleAsync();
        Assert.Multiple(() =>
        {
            Assert.That(directory.CustomerId, Is.EqualTo(customer.Id.ToString()));
            Assert.That(directory.Name, Is.EqualTo("Acme Corp"));
            Assert.That(directory.AdminCount, Is.EqualTo(1));
        });
    }

    [Test]
    public async Task SqlRepository_UpdateAsync_RefreshesDirectory()
    {
        await using var context = CreateCustomersDbContext();
        var repository = new SqlCustomerRepository(context);
        var customer = Customer.Register(UlidId.NewUlid(), "Acme Corp", DateTimeOffset.UtcNow);
        var firstAdmin = UlidId.NewUlid();
        customer.AssignAdmin(firstAdmin, firstAdmin, DateTimeOffset.UtcNow);

        await repository.AddAsync(customer, CancellationToken.None);
        await context.SaveChangesAsync(CancellationToken.None);

        customer.Rename("Acme Holdings", DateTimeOffset.UtcNow);
        var secondAdmin = UlidId.NewUlid();
        customer.AssignAdmin(secondAdmin, secondAdmin, DateTimeOffset.UtcNow);

        await repository.UpdateAsync(customer, CancellationToken.None);
        await context.SaveChangesAsync(CancellationToken.None);

        var directory = await context.CustomerDirectory.SingleAsync();
        Assert.Multiple(() =>
        {
            Assert.That(directory.Name, Is.EqualTo("Acme Holdings"));
            Assert.That(directory.AdminCount, Is.EqualTo(2));
        });
    }

    [Test]
    public async Task SqlRepository_RemoveAdmin_UpdatesDirectoryCount()
    {
        await using var context = CreateCustomersDbContext();
        var repository = new SqlCustomerRepository(context);
        var customer = Customer.Register(UlidId.NewUlid(), "Acme Corp", DateTimeOffset.UtcNow);
        var admin1 = UlidId.NewUlid();
        var admin2 = UlidId.NewUlid();
        customer.AssignAdmin(admin1, admin1, DateTimeOffset.UtcNow);
        customer.AssignAdmin(admin2, admin2, DateTimeOffset.UtcNow);

        await repository.AddAsync(customer, CancellationToken.None);
        await context.SaveChangesAsync(CancellationToken.None);

        customer.RemoveAdmin(admin1, admin2, DateTimeOffset.UtcNow);
        await repository.UpdateAsync(customer, CancellationToken.None);
        await context.SaveChangesAsync(CancellationToken.None);

        var directory = await context.CustomerDirectory.SingleAsync();
        Assert.That(directory.AdminCount, Is.EqualTo(1));
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
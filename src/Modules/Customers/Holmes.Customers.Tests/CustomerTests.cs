using Holmes.Core.Domain.ValueObjects;
using Holmes.Customers.Domain;

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
}
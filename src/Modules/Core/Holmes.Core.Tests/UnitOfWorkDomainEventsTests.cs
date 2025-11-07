using Holmes.Core.Domain;
using Holmes.Core.Infrastructure.Sql;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace Holmes.Core.Tests;

[TestFixture]
public class UnitOfWorkDomainEventsTests
{
    private class TestEvent : INotification;

    private class FakeDbContext(DbContextOptions<FakeDbContext> options)
        : DbContext(options);

    private sealed class FakeAggregate : IHasDomainEvents
    {
        private readonly List<INotification> _events = [];

        public IReadOnlyCollection<INotification> DomainEvents => _events;

        public void ClearDomainEvents()
        {
            _events.Clear();
        }

        public void Add(INotification @event)
        {
            _events.Add(@event);
        }
    }

    private sealed class FakeUnitOfWork(FakeDbContext db, IMediator mediator)
        : UnitOfWork<FakeDbContext>(db, mediator)
    {
    }

    [Test]
    public async Task Publishes_DomainEvents_After_Successful_Save()
    {
        var dbOptions = new DbContextOptionsBuilder<FakeDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        await using var ctx = new FakeDbContext(dbOptions);

        var published = new List<INotification>();
        var mediator = new Mock<IMediator>();
        mediator
            .Setup(m => m.Publish(It.IsAny<INotification>(), It.IsAny<CancellationToken>()))
            .Callback<INotification, CancellationToken>((e, _) => published.Add(e))
            .Returns(Task.CompletedTask);

        var aggregate = new FakeAggregate();
        aggregate.Add(new TestEvent());

        using var uow = new FakeUnitOfWork(ctx, mediator.Object);
        uow.RegisterDomainEvents(aggregate);

        await uow.SaveChangesAsync(CancellationToken.None);

        Assert.That(published, Has.Count.EqualTo(1));
        Assert.That(published.Single(), Is.TypeOf<TestEvent>());
        Assert.That(aggregate.DomainEvents, Is.Empty);
    }
}

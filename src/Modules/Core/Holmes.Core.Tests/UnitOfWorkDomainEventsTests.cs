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

    private class ThrowingDbContext(DbContextOptions<FakeDbContext> options)
        : FakeDbContext(options)
    {
        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            throw new InvalidOperationException("boom");
        }
    }

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
        : UnitOfWork<FakeDbContext>(db, mediator);

    private sealed class ThrowingUnitOfWork(ThrowingDbContext db, IMediator mediator)
        : UnitOfWork<ThrowingDbContext>(db, mediator);

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

    [Test]
    public async Task Publishes_All_Aggregate_Events()
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

        var aggregate1 = new FakeAggregate();
        aggregate1.Add(new TestEvent());
        var aggregate2 = new FakeAggregate();
        aggregate2.Add(new TestEvent());

        using var uow = new FakeUnitOfWork(ctx, mediator.Object);
        uow.RegisterDomainEvents(new[] { aggregate1, aggregate2 });

        await uow.SaveChangesAsync(CancellationToken.None);

        Assert.That(published, Has.Count.EqualTo(2));
        Assert.That(aggregate1.DomainEvents, Is.Empty);
        Assert.That(aggregate2.DomainEvents, Is.Empty);
    }

    [Test]
    public async Task Does_Not_Publish_When_Save_Fails()
    {
        var dbOptions = new DbContextOptionsBuilder<FakeDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        await using var ctx = new ThrowingDbContext(dbOptions);

        var mediator = new Mock<IMediator>(MockBehavior.Strict);
        using var uow = new ThrowingUnitOfWork(ctx, mediator.Object);
        var aggregate = new FakeAggregate();
        aggregate.Add(new TestEvent());
        uow.RegisterDomainEvents(aggregate);

        Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await uow.SaveChangesAsync(CancellationToken.None));
        mediator.Verify(m => m.Publish(It.IsAny<INotification>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}

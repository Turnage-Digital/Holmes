using Holmes.Core.Domain;
using Holmes.Core.Infrastructure.Sql;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace Holmes.Core.Tests;

[TestFixture]
public class UnitOfWorkEventPhaseTests
{
    private class BeforeEvent : INotification;

    private class AfterEvent : INotification;

    private class FakeDbContext(DbContextOptions<FakeDbContext> options)
        : DbContext(options);

    private class FakeUnitOfWork(FakeDbContext db, IMediator mediator, IDomainEventQueue queue)
        : UnitOfWork<FakeDbContext>(db, mediator, queue);

    [Test]
    public async Task Publishes_BeforeSave_Then_AfterSave_In_Order()
    {
        var dbOptions = new DbContextOptionsBuilder<FakeDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        await using var ctx = new FakeDbContext(dbOptions);

        var published = new List<string>();
        var mediator = new Mock<IMediator>();
        mediator
            .Setup(m => m.Publish(It.IsAny<INotification>(), It.IsAny<CancellationToken>()))
            .Callback<INotification, CancellationToken>((e, _) => published.Add(e.GetType().Name))
            .Returns(Task.CompletedTask);

        var queue = new DomainEventQueue();
        queue.Enqueue(new BeforeEvent(), EventPhase.BeforeSave);
        queue.Enqueue(new AfterEvent(), EventPhase.AfterSave);

        using var uow = new FakeUnitOfWork(ctx, mediator.Object, queue);
        await uow.SaveChangesAsync(CancellationToken.None);

        Assert.That(published, Is.EqualTo([nameof(BeforeEvent), nameof(AfterEvent)]));
    }
}
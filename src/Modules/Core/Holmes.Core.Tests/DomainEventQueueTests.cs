using Holmes.Core.Domain;
using Holmes.Core.Infrastructure.Sql;
using MediatR;

namespace Holmes.Core.Tests;

[TestFixture]
public class DomainEventQueueTests
{
    private class BeforeEvent : INotification;

    private class AfterEvent : INotification;

    [Test]
    public void Enqueue_Dequeue_ByPhase_ClearsQueue()
    {
        var queue = new DomainEventQueue();
        var before = new BeforeEvent();
        var after = new AfterEvent();

        queue.Enqueue(before, EventPhase.BeforeSave);
        queue.Enqueue(after, EventPhase.AfterSave);

        var be = queue.Dequeue(EventPhase.BeforeSave);
        var ae = queue.Dequeue(EventPhase.AfterSave);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(be.Single(), Is.SameAs(before));
            Assert.That(ae.Single(), Is.SameAs(after));

            // queues are cleared after dequeue
            Assert.That(queue.Dequeue(EventPhase.BeforeSave), Is.Empty);
            Assert.That(queue.Dequeue(EventPhase.AfterSave), Is.Empty);
        }
    }
}
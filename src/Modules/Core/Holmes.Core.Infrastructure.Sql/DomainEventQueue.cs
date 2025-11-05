using Holmes.Core.Domain;
using MediatR;

namespace Holmes.Core.Infrastructure.Sql;

public class DomainEventQueue : IDomainEventQueue
{
    private readonly List<INotification> _after = [];
    private readonly List<INotification> _before = [];

    public void Enqueue(INotification @event, EventPhase phase)
    {
        switch (phase)
        {
            case EventPhase.BeforeSave:
                _before.Add(@event);
                break;
            case EventPhase.AfterSave:
                _after.Add(@event);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(phase), phase, null);
        }
    }

    public IReadOnlyCollection<INotification> Dequeue(EventPhase phase)
    {
        var selected = phase == EventPhase.BeforeSave ? _before : _after;
        var retval = selected.ToArray();
        selected.Clear();
        return retval;
    }
}
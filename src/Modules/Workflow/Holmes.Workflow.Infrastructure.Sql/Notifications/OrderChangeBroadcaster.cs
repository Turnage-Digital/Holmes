using System.Collections.Concurrent;
using System.Threading.Channels;
using Holmes.Workflow.Application.Notifications;

namespace Holmes.Workflow.Infrastructure.Sql.Notifications;

public sealed class OrderChangeBroadcaster : IOrderChangeBroadcaster
{
    private readonly ConcurrentDictionary<Guid, Channel<OrderChange>> _subscribers = new();

    public async Task PublishAsync(OrderChange change, CancellationToken cancellationToken)
    {
        foreach (var channel in _subscribers.Values)
        {
            await channel.Writer.WriteAsync(change, cancellationToken);
        }
    }

    public OrderChangeSubscription Subscribe()
    {
        var channel = Channel.CreateUnbounded<OrderChange>();
        var id = Guid.NewGuid();
        _subscribers[id] = channel;
        return new OrderChangeSubscription(id, channel.Reader);
    }

    public void Unsubscribe(Guid subscriptionId)
    {
        if (_subscribers.TryRemove(subscriptionId, out var channel))
        {
            channel.Writer.TryComplete();
        }
    }
}
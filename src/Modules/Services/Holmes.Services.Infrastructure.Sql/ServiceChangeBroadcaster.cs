using System.Collections.Concurrent;
using System.Threading.Channels;
using Holmes.Core.Domain.ValueObjects;
using Holmes.Services.Application.Abstractions;

namespace Holmes.Services.Infrastructure.Sql;

public sealed class ServiceChangeBroadcaster : IServiceChangeBroadcaster
{
    private const int HistoryLimit = 512;
    private readonly ConcurrentQueue<ServiceChange> _history = new();

    private readonly ConcurrentDictionary<Guid, Subscription> _subscribers = new();

    public async Task PublishAsync(ServiceChange change, CancellationToken cancellationToken)
    {
        _history.Enqueue(change);
        while (_history.Count > HistoryLimit && _history.TryDequeue(out _))
        {
        }

        foreach (var subscription in _subscribers.Values)
        {
            if (!subscription.ShouldReceive(change))
            {
                continue;
            }

            await subscription.Channel.Writer.WriteAsync(change, cancellationToken);
        }
    }

    public ServiceChangeSubscription Subscribe(
        IReadOnlyCollection<UlidId>? orderFilter,
        UlidId? lastEventId,
        CancellationToken cancellationToken
    )
    {
        var channel = Channel.CreateUnbounded<ServiceChange>();
        var id = Guid.NewGuid();
        var subscription = new Subscription(orderFilter, channel);
        _subscribers[id] = subscription;

        if (lastEventId is not null)
        {
            var backlog = _history.Where(change =>
                subscription.ShouldReceive(change) &&
                change.ChangeId.Value.CompareTo(lastEventId.Value) > 0);

            foreach (var change in backlog)
            {
                channel.Writer.TryWrite(change);
            }
        }

        cancellationToken.Register(() => Unsubscribe(id));
        return new ServiceChangeSubscription(id, channel.Reader);
    }

    public void Unsubscribe(Guid subscriptionId)
    {
        if (_subscribers.TryRemove(subscriptionId, out var subscription))
        {
            subscription.Channel.Writer.TryComplete();
        }
    }

    private sealed record Subscription(
        IReadOnlyCollection<UlidId>? OrderFilter,
        Channel<ServiceChange> Channel
    )
    {
        private readonly HashSet<UlidId>? _filterSet = OrderFilter is null
            ? null
            : [..OrderFilter];

        public bool ShouldReceive(ServiceChange change)
        {
            return _filterSet is null || _filterSet.Count == 0 || _filterSet.Contains(change.OrderId);
        }
    }
}
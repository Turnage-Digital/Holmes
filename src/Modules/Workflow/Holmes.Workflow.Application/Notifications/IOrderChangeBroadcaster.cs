using System.Threading.Channels;
using Holmes.Core.Domain.ValueObjects;
using Holmes.Workflow.Domain;

namespace Holmes.Workflow.Application.Notifications;

public interface IOrderChangeBroadcaster
{
    Task PublishAsync(OrderChange change, CancellationToken cancellationToken);

    OrderChangeSubscription Subscribe(
        IReadOnlyCollection<UlidId>? orderFilter,
        UlidId? lastEventId,
        CancellationToken cancellationToken
    );

    void Unsubscribe(Guid subscriptionId);
}

public sealed record OrderChange(
    UlidId ChangeId,
    UlidId OrderId,
    OrderStatus Status,
    string Reason,
    DateTimeOffset ChangedAt
);

public sealed record OrderChangeSubscription(
    Guid SubscriptionId,
    ChannelReader<OrderChange> Reader
);
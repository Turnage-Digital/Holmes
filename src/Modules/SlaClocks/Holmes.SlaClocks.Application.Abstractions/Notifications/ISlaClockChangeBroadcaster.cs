using System.Threading.Channels;
using Holmes.Core.Domain.ValueObjects;
using Holmes.SlaClocks.Domain;

namespace Holmes.SlaClocks.Application.Abstractions.Notifications;

public interface ISlaClockChangeBroadcaster
{
    Task PublishAsync(SlaClockChange change, CancellationToken cancellationToken);

    SlaClockChangeSubscription Subscribe(
        IReadOnlyCollection<UlidId>? orderFilter,
        UlidId? lastEventId,
        CancellationToken cancellationToken
    );

    void Unsubscribe(Guid subscriptionId);
}

public sealed record SlaClockChange(
    UlidId ChangeId,
    UlidId ClockId,
    UlidId OrderId,
    UlidId CustomerId,
    ClockKind Kind,
    ClockState State,
    string? Reason,
    DateTimeOffset ChangedAt
);

public sealed record SlaClockChangeSubscription(
    Guid SubscriptionId,
    ChannelReader<SlaClockChange> Reader
);

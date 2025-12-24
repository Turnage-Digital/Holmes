using System.Threading.Channels;
using Holmes.Core.Domain.ValueObjects;
using Holmes.Services.Domain;

namespace Holmes.Services.Application.Abstractions;

public interface IServiceChangeBroadcaster
{
    Task PublishAsync(ServiceChange change, CancellationToken cancellationToken);

    ServiceChangeSubscription Subscribe(
        IReadOnlyCollection<UlidId>? orderFilter,
        UlidId? lastEventId,
        CancellationToken cancellationToken
    );

    void Unsubscribe(Guid subscriptionId);
}

public sealed record ServiceChange(
    UlidId ChangeId,
    UlidId ServiceId,
    UlidId OrderId,
    string ServiceTypeCode,
    ServiceStatus Status,
    string? Reason,
    DateTimeOffset ChangedAt
);

public sealed record ServiceChangeSubscription(
    Guid SubscriptionId,
    ChannelReader<ServiceChange> Reader
);
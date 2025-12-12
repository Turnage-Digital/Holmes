using MediatR;

namespace Holmes.Core.Application.Abstractions.Events;

/// <summary>
///     Serializes and deserializes domain events for persistence.
/// </summary>
public interface IDomainEventSerializer
{
    /// <summary>
    ///     Serializes a domain event into an envelope ready for persistence.
    /// </summary>
    EventEnvelope Serialize(
        INotification @event,
        string? correlationId,
        string? causationId,
        string? actorId
    );

    /// <summary>
    ///     Deserializes a stored event payload back into a domain event.
    /// </summary>
    INotification Deserialize(string payload, string eventName);

    /// <summary>
    ///     Gets the fully qualified type name for an event.
    /// </summary>
    string GetEventName(INotification @event);
}
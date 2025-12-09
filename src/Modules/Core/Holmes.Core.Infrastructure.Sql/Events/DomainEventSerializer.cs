using System.Collections.Concurrent;
using System.Text.Json;
using System.Text.Json.Serialization;
using Holmes.Core.Application.Abstractions.Events;
using MediatR;

namespace Holmes.Core.Infrastructure.Sql.Events;

/// <summary>
/// JSON-based domain event serializer with lazy type lookup for deserialization.
/// Uses assembly-qualified type names stored in the event name for reliable cross-assembly lookup.
/// </summary>
public sealed class DomainEventSerializer : IDomainEventSerializer
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };

    // Cache resolved types for performance
    private readonly ConcurrentDictionary<string, Type> _typeCache = new();

    public EventEnvelope Serialize(
        INotification @event,
        string? correlationId,
        string? causationId,
        string? actorId)
    {
        var eventName = GetEventName(@event);
        var payload = JsonSerializer.Serialize(@event, @event.GetType(), SerializerOptions);

        return new EventEnvelope(
            Ulid.NewUlid().ToString(),
            eventName,
            payload,
            correlationId,
            causationId,
            actorId);
    }

    public INotification Deserialize(string payload, string eventName)
    {
        var type = ResolveType(eventName);

        var result = JsonSerializer.Deserialize(payload, type, SerializerOptions);
        if (result is not INotification notification)
        {
            throw new InvalidOperationException($"Failed to deserialize event: {eventName}");
        }

        return notification;
    }

    public string GetEventName(INotification @event) =>
        @event.GetType().AssemblyQualifiedName!;

    private Type ResolveType(string eventName)
    {
        return _typeCache.GetOrAdd(eventName, name =>
        {
            // First try assembly-qualified name (preferred - includes assembly info)
            var type = Type.GetType(name);
            if (type is not null)
            {
                return type;
            }

            // Fallback: scan loaded assemblies for the type by full name
            // This handles cases where we have just the namespace-qualified name
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                type = assembly.GetType(name);
                if (type is not null)
                {
                    return type;
                }
            }

            throw new InvalidOperationException(
                $"Unknown event type: {name}. Ensure the assembly containing this event is loaded.");
        });
    }
}

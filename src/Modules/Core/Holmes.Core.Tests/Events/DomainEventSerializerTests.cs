using Holmes.Core.Infrastructure.Sql.Events;
using MediatR;

namespace Holmes.Core.Tests.Events;

[TestFixture]
public class DomainEventSerializerTests
{
    // Test event for serialization tests
    public sealed record TestDomainEvent(string Id, string Name, int Value) : INotification;

    public sealed record EventWithNullableProperties(string Id, string? OptionalName, int? OptionalValue) : INotification;

    [Test]
    public void Serialize_CreatesValidEnvelope()
    {
        var serializer = new DomainEventSerializer();
        var @event = new TestDomainEvent("123", "Test", 42);

        var envelope = serializer.Serialize(@event, "corr-1", "cause-1", "user-1");

        Assert.Multiple(() =>
        {
            Assert.That(envelope.EventId, Is.Not.Null.And.Not.Empty);
            Assert.That(envelope.EventName, Does.Contain("TestDomainEvent"));
            Assert.That(envelope.Payload, Does.Contain("\"id\":\"123\""));
            Assert.That(envelope.Payload, Does.Contain("\"name\":\"Test\""));
            Assert.That(envelope.Payload, Does.Contain("\"value\":42"));
            Assert.That(envelope.CorrelationId, Is.EqualTo("corr-1"));
            Assert.That(envelope.CausationId, Is.EqualTo("cause-1"));
            Assert.That(envelope.ActorId, Is.EqualTo("user-1"));
        });
    }

    [Test]
    public void Serialize_UsesAssemblyQualifiedName()
    {
        var serializer = new DomainEventSerializer();
        var @event = new TestDomainEvent("123", "Test", 42);

        var envelope = serializer.Serialize(@event, null, null, null);

        // Should contain assembly name for reliable deserialization
        Assert.That(envelope.EventName, Does.Contain("Holmes.Core.Tests"));
    }

    [Test]
    public void Deserialize_RestoresOriginalEvent()
    {
        var serializer = new DomainEventSerializer();
        var original = new TestDomainEvent("123", "Test", 42);

        var envelope = serializer.Serialize(original, null, null, null);
        var restored = serializer.Deserialize(envelope.Payload, envelope.EventName);

        Assert.That(restored, Is.TypeOf<TestDomainEvent>());
        var typedRestored = (TestDomainEvent)restored;
        Assert.Multiple(() =>
        {
            Assert.That(typedRestored.Id, Is.EqualTo("123"));
            Assert.That(typedRestored.Name, Is.EqualTo("Test"));
            Assert.That(typedRestored.Value, Is.EqualTo(42));
        });
    }

    [Test]
    public void Deserialize_HandlesNullProperties()
    {
        var serializer = new DomainEventSerializer();
        var original = new EventWithNullableProperties("123", null, null);

        var envelope = serializer.Serialize(original, null, null, null);
        var restored = serializer.Deserialize(envelope.Payload, envelope.EventName);

        Assert.That(restored, Is.TypeOf<EventWithNullableProperties>());
        var typedRestored = (EventWithNullableProperties)restored;
        Assert.Multiple(() =>
        {
            Assert.That(typedRestored.Id, Is.EqualTo("123"));
            Assert.That(typedRestored.OptionalName, Is.Null);
            Assert.That(typedRestored.OptionalValue, Is.Null);
        });
    }

    [Test]
    public void Deserialize_ThrowsForUnknownType()
    {
        var serializer = new DomainEventSerializer();

        Assert.Throws<InvalidOperationException>(() =>
            serializer.Deserialize("{}", "NonExistent.Event.Type, NonExistent.Assembly"));
    }

    [Test]
    public void GetEventName_ReturnsAssemblyQualifiedName()
    {
        var serializer = new DomainEventSerializer();
        var @event = new TestDomainEvent("123", "Test", 42);

        var name = serializer.GetEventName(@event);

        Assert.That(name, Is.EqualTo(@event.GetType().AssemblyQualifiedName));
    }

    [Test]
    public void RoundTrip_PreservesAllData()
    {
        var serializer = new DomainEventSerializer();
        var original = new TestDomainEvent("abc-123", "Complex Name With Spaces", -999);

        var envelope = serializer.Serialize(original, "corr", "cause", "actor");
        var restored = (TestDomainEvent)serializer.Deserialize(envelope.Payload, envelope.EventName);

        Assert.That(restored, Is.EqualTo(original));
    }
}

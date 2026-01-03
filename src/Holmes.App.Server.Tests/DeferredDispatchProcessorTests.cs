using Holmes.App.Server.Services;
using Holmes.Core.Contracts.Events;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;

namespace Holmes.App.Server.Tests;

/// <summary>
///     Unit tests for <see cref="DeferredDispatchProcessor" />.
///     Verifies the background processor correctly polls for undispatched events,
///     deserializes them, dispatches via MediatR, and marks them as dispatched.
/// </summary>
[TestFixture]
public class DeferredDispatchProcessorTests
{
    [SetUp]
    public void SetUp()
    {
        _eventStore = new Mock<IEventStore>();
        _serializer = new Mock<IDomainEventSerializer>();
        _publisher = new Mock<IPublisher>();
        _logger = new Mock<ILogger<DeferredDispatchProcessor>>();

        // Mock IMediator as IPublisher since that's what MediatR uses for Publish
        var mediatorMock = _publisher.As<IMediator>();

        // Build a real ServiceProvider to provide scoping
        var services = new ServiceCollection();
        services.AddSingleton(_eventStore.Object);
        services.AddSingleton(_serializer.Object);
        services.AddSingleton(mediatorMock.Object);
        var sp = services.BuildServiceProvider();
        _scopeFactory = sp.GetRequiredService<IServiceScopeFactory>();
    }

    private Mock<IEventStore> _eventStore = null!;
    private Mock<IDomainEventSerializer> _serializer = null!;
    private Mock<IPublisher> _publisher = null!;
    private Mock<ILogger<DeferredDispatchProcessor>> _logger = null!;
    private IServiceScopeFactory _scopeFactory = null!;

    private class TestDomainEvent : INotification;

    private static StoredEvent CreateStoredEvent(long position, string streamId, string eventName, string payload)
    {
        return new StoredEvent(
            position,
            streamId,
            streamId.Split(':')[0],
            1,
            Guid.NewGuid().ToString(),
            eventName,
            payload,
            DateTime.UtcNow,
            null,
            null,
            null);
    }

    [Test]
    public async Task Dispatches_Pending_Events_And_Marks_As_Dispatched()
    {
        // Arrange
        var storedEvent = CreateStoredEvent(1, "Order:123", "OrderCreated", "{\"orderId\":\"123\"}");
        var domainEvent = new TestDomainEvent();
        var markedPositions = new List<long>();

        _eventStore
            .Setup(e => e.ReadUndispatchedAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([storedEvent]);

        _serializer
            .Setup(s => s.Deserialize(storedEvent.Payload, storedEvent.EventName))
            .Returns(domainEvent);

        // Use generic setup for IPublisher
        _publisher
            .Setup(p => p.Publish(It.IsAny<INotification>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _eventStore
            .Setup(e => e.MarkDispatchedBatchAsync(It.IsAny<IEnumerable<long>>(), It.IsAny<CancellationToken>()))
            .Callback<IEnumerable<long>, CancellationToken>((positions, _) => markedPositions.AddRange(positions))
            .Returns(Task.CompletedTask);

        // Act
        var processor = new DeferredDispatchProcessor(_scopeFactory, _logger.Object);
        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(300));

        try
        {
            await processor.StartAsync(cts.Token);
            await Task.Delay(100, cts.Token);
            await processor.StopAsync(CancellationToken.None);
        }
        catch (OperationCanceledException)
        {
        }

        // Assert - Event should be marked as dispatched
        Assert.That(markedPositions, Contains.Item(1L), "Position 1 should be marked as dispatched");
    }

    [Test]
    public async Task Continues_Processing_When_Single_Event_Fails()
    {
        // Arrange
        var storedEvent1 = CreateStoredEvent(1, "Order:111", "OrderCreated", "{\"orderId\":\"111\"}");
        var storedEvent2 = CreateStoredEvent(2, "Order:222", "OrderCreated", "{\"orderId\":\"222\"}");

        var domainEvent1 = new TestDomainEvent();
        var domainEvent2 = new TestDomainEvent();
        var markedPositions = new List<long>();

        _eventStore
            .Setup(e => e.ReadUndispatchedAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([storedEvent1, storedEvent2]);

        _serializer
            .Setup(s => s.Deserialize(storedEvent1.Payload, storedEvent1.EventName))
            .Returns(domainEvent1);

        _serializer
            .Setup(s => s.Deserialize(storedEvent2.Payload, storedEvent2.EventName))
            .Returns(domainEvent2);

        // First event (domainEvent1) fails, second event (domainEvent2) succeeds
        // Use SetupSequence to control the order
        _publisher
            .SetupSequence(p => p.Publish(It.IsAny<INotification>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Handler failed"))
            .Returns(Task.CompletedTask);

        _eventStore
            .Setup(e => e.MarkDispatchedBatchAsync(It.IsAny<IEnumerable<long>>(), It.IsAny<CancellationToken>()))
            .Callback<IEnumerable<long>, CancellationToken>((positions, _) => markedPositions.AddRange(positions))
            .Returns(Task.CompletedTask);

        // Act
        var processor = new DeferredDispatchProcessor(_scopeFactory, _logger.Object);
        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(300));

        try
        {
            await processor.StartAsync(cts.Token);
            await Task.Delay(100, cts.Token);
            await processor.StopAsync(CancellationToken.None);
        }
        catch (OperationCanceledException)
        {
        }

        // Assert - Only position 2 marked (position 1 failed)
        Assert.That(markedPositions, Contains.Item(2L), "Position 2 should be marked");
        Assert.That(markedPositions, Does.Not.Contain(1L), "Position 1 should NOT be marked (failed)");
    }

    [Test]
    public async Task Does_Not_Mark_Dispatched_When_All_Events_Fail()
    {
        // Arrange
        var storedEvent = CreateStoredEvent(1, "Order:123", "OrderCreated", "{\"orderId\":\"123\"}");
        var domainEvent = new TestDomainEvent();
        var markedPositions = new List<long>();

        _eventStore
            .Setup(e => e.ReadUndispatchedAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([storedEvent]);

        _serializer
            .Setup(s => s.Deserialize(storedEvent.Payload, storedEvent.EventName))
            .Returns(domainEvent);

        _publisher
            .Setup(p => p.Publish(It.IsAny<INotification>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Handler failed"));

        _eventStore
            .Setup(e => e.MarkDispatchedBatchAsync(It.IsAny<IEnumerable<long>>(), It.IsAny<CancellationToken>()))
            .Callback<IEnumerable<long>, CancellationToken>((positions, _) => markedPositions.AddRange(positions))
            .Returns(Task.CompletedTask);

        // Act
        var processor = new DeferredDispatchProcessor(_scopeFactory, _logger.Object);
        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(300));

        try
        {
            await processor.StartAsync(cts.Token);
            await Task.Delay(100, cts.Token);
            await processor.StopAsync(CancellationToken.None);
        }
        catch (OperationCanceledException)
        {
        }

        // Assert - Nothing should be marked when all events fail
        Assert.That(markedPositions, Is.Empty, "No positions should be marked when all events fail");
    }

    [Test]
    public async Task Does_Nothing_When_No_Undispatched_Events()
    {
        // Arrange
        var markedPositions = new List<long>();

        _eventStore
            .Setup(e => e.ReadUndispatchedAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        _eventStore
            .Setup(e => e.MarkDispatchedBatchAsync(It.IsAny<IEnumerable<long>>(), It.IsAny<CancellationToken>()))
            .Callback<IEnumerable<long>, CancellationToken>((positions, _) => markedPositions.AddRange(positions))
            .Returns(Task.CompletedTask);

        // Act
        var processor = new DeferredDispatchProcessor(_scopeFactory, _logger.Object);
        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(300));

        try
        {
            await processor.StartAsync(cts.Token);
            await Task.Delay(100, cts.Token);
            await processor.StopAsync(CancellationToken.None);
        }
        catch (OperationCanceledException)
        {
        }

        // Assert - Nothing to mark
        Assert.That(markedPositions, Is.Empty);

        // Verify Publish was never called
        _publisher.Verify(
            p => p.Publish(It.IsAny<INotification>(), It.IsAny<CancellationToken>()),
            Times.Never());
    }

    [Test]
    public async Task Handles_Deserialization_Errors_Gracefully()
    {
        // Arrange
        var storedEvent1 = CreateStoredEvent(1, "Order:111", "BrokenEvent", "not valid json");
        var storedEvent2 = CreateStoredEvent(2, "Order:222", "OrderCreated", "{\"orderId\":\"222\"}");

        var domainEvent2 = new TestDomainEvent();
        var markedPositions = new List<long>();

        _eventStore
            .Setup(e => e.ReadUndispatchedAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([storedEvent1, storedEvent2]);

        // First event fails deserialization
        _serializer
            .Setup(s => s.Deserialize(storedEvent1.Payload, storedEvent1.EventName))
            .Throws(new InvalidOperationException("Deserialization failed"));

        _serializer
            .Setup(s => s.Deserialize(storedEvent2.Payload, storedEvent2.EventName))
            .Returns(domainEvent2);

        _publisher
            .Setup(p => p.Publish(It.IsAny<INotification>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _eventStore
            .Setup(e => e.MarkDispatchedBatchAsync(It.IsAny<IEnumerable<long>>(), It.IsAny<CancellationToken>()))
            .Callback<IEnumerable<long>, CancellationToken>((positions, _) => markedPositions.AddRange(positions))
            .Returns(Task.CompletedTask);

        // Act
        var processor = new DeferredDispatchProcessor(_scopeFactory, _logger.Object);
        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(300));

        try
        {
            await processor.StartAsync(cts.Token);
            await Task.Delay(100, cts.Token);
            await processor.StopAsync(CancellationToken.None);
        }
        catch (OperationCanceledException)
        {
        }

        // Assert - Only position 2 should be marked (position 1 failed deserialization)
        Assert.That(markedPositions, Contains.Item(2L), "Position 2 should be marked");
        Assert.That(markedPositions, Does.Not.Contain(1L), "Position 1 should NOT be marked (deserialization failed)");
    }
}
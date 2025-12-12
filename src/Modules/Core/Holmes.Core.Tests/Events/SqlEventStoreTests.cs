using Holmes.Core.Application.Abstractions.Events;
using Holmes.Core.Infrastructure.Sql;
using Holmes.Core.Infrastructure.Sql.Events;
using Microsoft.EntityFrameworkCore;

namespace Holmes.Core.Tests.Events;

[TestFixture]
public class SqlEventStoreTests
{
    [Test]
    public async Task AppendEventsAsync_PersistsEventsToDatabase()
    {
        await using var context = CreateCoreDbContext();
        var store = new SqlEventStore(context);

        var events = new List<EventEnvelope>
        {
            new("evt-1", "TestEvent", "{\"data\":\"test1\"}", "corr-1", null, "user-1"),
            new("evt-2", "TestEvent", "{\"data\":\"test2\"}", "corr-1", "evt-1", "user-1")
        };

        await store.AppendEventsAsync("tenant-1", "Order:123", "Order", events, CancellationToken.None);

        var records = await context.Events.ToListAsync();
        Assert.That(records, Has.Count.EqualTo(2));

        var first = records.First();
        Assert.Multiple(() =>
        {
            Assert.That(first.TenantId, Is.EqualTo("tenant-1"));
            Assert.That(first.StreamId, Is.EqualTo("Order:123"));
            Assert.That(first.StreamType, Is.EqualTo("Order"));
            Assert.That(first.EventId, Is.EqualTo("evt-1"));
            Assert.That(first.Name, Is.EqualTo("TestEvent"));
            Assert.That(first.Payload, Is.EqualTo("{\"data\":\"test1\"}"));
            Assert.That(first.Version, Is.EqualTo(1));
        });

        var second = records.Last();
        Assert.Multiple(() =>
        {
            Assert.That(second.Version, Is.EqualTo(2));
            Assert.That(second.CausationId, Is.EqualTo("evt-1"));
        });
    }

    [Test]
    public async Task AppendEventsAsync_IncrementsVersionFromExisting()
    {
        await using var context = CreateCoreDbContext();
        var store = new SqlEventStore(context);

        // First batch
        var batch1 = new List<EventEnvelope> { new("evt-1", "TestEvent", "{}", null, null, null) };
        await store.AppendEventsAsync("tenant-1", "Order:123", "Order", batch1, CancellationToken.None);

        // Second batch should start at version 2
        var batch2 = new List<EventEnvelope> { new("evt-2", "TestEvent", "{}", null, null, null) };
        await store.AppendEventsAsync("tenant-1", "Order:123", "Order", batch2, CancellationToken.None);

        var records = await context.Events.OrderBy(e => e.Version).ToListAsync();
        Assert.That(records[0].Version, Is.EqualTo(1));
        Assert.That(records[1].Version, Is.EqualTo(2));
    }

    [Test]
    public async Task ReadStreamAsync_ReturnsEventsForStream()
    {
        await using var context = CreateCoreDbContext();
        var store = new SqlEventStore(context);

        // Add events for two different streams
        var events1 = new List<EventEnvelope> { new("evt-1", "TestEvent", "{}", null, null, null) };
        var events2 = new List<EventEnvelope> { new("evt-2", "TestEvent", "{}", null, null, null) };

        await store.AppendEventsAsync("tenant-1", "Order:123", "Order", events1, CancellationToken.None);
        await store.AppendEventsAsync("tenant-1", "Order:456", "Order", events2, CancellationToken.None);

        var result = await store.ReadStreamAsync("tenant-1", "Order:123", 0, 100, CancellationToken.None);

        Assert.That(result, Has.Count.EqualTo(1));
        Assert.That(result[0].EventId, Is.EqualTo("evt-1"));
    }

    [Test]
    public async Task ReadByStreamTypeAsync_ReturnsEventsForStreamType()
    {
        await using var context = CreateCoreDbContext();
        var store = new SqlEventStore(context);

        var orderEvents = new List<EventEnvelope> { new("evt-1", "OrderCreated", "{}", null, null, null) };
        var userEvents = new List<EventEnvelope> { new("evt-2", "UserCreated", "{}", null, null, null) };

        await store.AppendEventsAsync("tenant-1", "Order:123", "Order", orderEvents, CancellationToken.None);
        await store.AppendEventsAsync("tenant-1", "User:456", "User", userEvents, CancellationToken.None);

        var result = await store.ReadByStreamTypeAsync("tenant-1", "Order", 0, 100, null, CancellationToken.None);

        Assert.That(result, Has.Count.EqualTo(1));
        Assert.That(result[0].StreamType, Is.EqualTo("Order"));
    }

    [Test]
    public async Task ReadByStreamTypeAsync_RespectsAsOfTimestamp()
    {
        await using var context = CreateCoreDbContext();
        var store = new SqlEventStore(context);

        var events = new List<EventEnvelope> { new("evt-1", "TestEvent", "{}", null, null, null) };
        await store.AppendEventsAsync("tenant-1", "Order:123", "Order", events, CancellationToken.None);

        // Query with timestamp in the past (before the event was created)
        var pastTimestamp = DateTime.UtcNow.AddHours(-1);
        var result =
            await store.ReadByStreamTypeAsync("tenant-1", "Order", 0, 100, pastTimestamp, CancellationToken.None);

        Assert.That(result, Is.Empty);
    }

    [Test]
    public async Task AppendEventsAsync_DoesNothingForEmptyEventList()
    {
        await using var context = CreateCoreDbContext();
        var store = new SqlEventStore(context);

        await store.AppendEventsAsync("tenant-1", "Order:123", "Order", [], CancellationToken.None);

        var records = await context.Events.ToListAsync();
        Assert.That(records, Is.Empty);
    }

    private static CoreDbContext CreateCoreDbContext()
    {
        var options = new DbContextOptionsBuilder<CoreDbContext>()
            .UseInMemoryDatabase($"core-tests-{Guid.NewGuid()}")
            .Options;
        return new CoreDbContext(options);
    }
}
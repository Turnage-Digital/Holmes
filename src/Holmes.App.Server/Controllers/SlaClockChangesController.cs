using System.Text.Json;
using Holmes.Core.Domain.ValueObjects;
using Holmes.SlaClocks.Application.Abstractions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Holmes.App.Server.Controllers;

[ApiController]
[Authorize]
[Route("api/clocks/sla/changes")]
public sealed class SlaClockChangesController(ISlaClockChangeBroadcaster broadcaster) : ControllerBase
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);
    private static readonly TimeSpan HeartbeatInterval = TimeSpan.FromSeconds(10);

    /// <summary>
    ///     SSE endpoint for real-time SLA clock status updates.
    ///     Filter by orderId query parameter to receive updates for specific orders only.
    /// </summary>
    [HttpGet]
    public async Task GetAsync(CancellationToken cancellationToken)
    {
        Response.Headers.CacheControl = "no-store";
        Response.Headers.ContentType = "text/event-stream";

        var orderFilter = ParseOrderFilter();
        var lastEventId = ParseLastEventId();

        var subscription = broadcaster.Subscribe(orderFilter, lastEventId, cancellationToken);

        try
        {
            using var heartbeatCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            var heartbeatTask = SendHeartbeatsAsync(heartbeatCts.Token);

            await foreach (var change in subscription.Reader.ReadAllAsync(heartbeatCts.Token))
            {
                var payload = JsonSerializer.Serialize(new
                {
                    clockId = change.ClockId.ToString(),
                    orderId = change.OrderId.ToString(),
                    customerId = change.CustomerId.ToString(),
                    kind = change.Kind.ToString(),
                    state = change.State.ToString(),
                    reason = change.Reason,
                    changedAt = change.ChangedAt
                }, SerializerOptions);

                await Response.WriteAsync($"id: {change.ChangeId}\n", heartbeatCts.Token);
                await Response.WriteAsync("event: clock.change\n", heartbeatCts.Token);
                await Response.WriteAsync($"data: {payload}\n\n", heartbeatCts.Token);
                await Response.Body.FlushAsync(heartbeatCts.Token);
            }

            await heartbeatCts.CancelAsync();
            await heartbeatTask;
        }
        catch (OperationCanceledException)
        {
            // Client disconnected - this is expected behavior for SSE
        }
        finally
        {
            broadcaster.Unsubscribe(subscription.SubscriptionId);
        }
    }

    private IReadOnlyCollection<UlidId>? ParseOrderFilter()
    {
        var values = Request.Query["orderId"];
        if (values.Count is 0)
        {
            return null;
        }

        var list = new List<UlidId>(values.Count);
        foreach (var value in values)
        {
            if (Ulid.TryParse(value, out var parsed))
            {
                list.Add(UlidId.FromUlid(parsed));
            }
        }

        return list;
    }

    private UlidId? ParseLastEventId()
    {
        var candidate = Request.Headers["Last-Event-ID"].FirstOrDefault()
                        ?? Request.Query["lastEventId"].FirstOrDefault();

        if (candidate is null)
        {
            return null;
        }

        return Ulid.TryParse(candidate, out var parsed) ? UlidId.FromUlid(parsed) : null;
    }

    private async Task SendHeartbeatsAsync(CancellationToken cancellationToken)
    {
        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                await Task.Delay(HeartbeatInterval, cancellationToken);
                await Response.WriteAsync(": heartbeat\n\n", cancellationToken);
                await Response.Body.FlushAsync(cancellationToken);
            }
        }
        catch (OperationCanceledException)
        {
        }
    }
}
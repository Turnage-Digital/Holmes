using System.Text.Json;
using Holmes.Workflow.Application.Notifications;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Holmes.App.Server.Controllers;

[ApiController]
[Authorize]
[Route("api/orders/changes")]
public sealed class OrderChangesController(IOrderChangeBroadcaster broadcaster) : ControllerBase
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    [HttpGet]
    public async Task GetAsync(CancellationToken cancellationToken)
    {
        Response.Headers.CacheControl = "no-store";
        Response.Headers.ContentType = "text/event-stream";

        var subscription = broadcaster.Subscribe();
        try
        {
            await foreach (var change in subscription.Reader.ReadAllAsync(cancellationToken))
            {
                var payload = JsonSerializer.Serialize(new
                {
                    orderId = change.OrderId.ToString(),
                    status = change.Status.ToString(),
                    reason = change.Reason,
                    changedAt = change.ChangedAt
                }, SerializerOptions);

                await Response.WriteAsync($"data: {payload}\n\n", cancellationToken);
                await Response.Body.FlushAsync(cancellationToken);
            }
        }
        finally
        {
            broadcaster.Unsubscribe(subscription.SubscriptionId);
        }
    }
}
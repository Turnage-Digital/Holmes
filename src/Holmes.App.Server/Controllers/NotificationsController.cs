using Holmes.App.Infrastructure.Security;
using Holmes.Core.Domain.ValueObjects;
using Holmes.Notifications.Application.Abstractions.Dtos;
using Holmes.Notifications.Application.Commands;
using Holmes.Notifications.Application.Queries;
using Holmes.Workflow.Application.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Holmes.App.Server.Controllers;

[ApiController]
[Authorize]
[Route("api/notifications")]
public sealed class NotificationsController(
    IMediator mediator,
    ICurrentUserAccess currentUserAccess
) : ControllerBase
{
    /// <summary>
    ///     Gets all notifications for a specific order.
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<NotificationSummaryDto>>> GetByOrderIdAsync(
        [FromQuery] string orderId,
        CancellationToken cancellationToken
    )
    {
        if (string.IsNullOrWhiteSpace(orderId))
        {
            return BadRequest("orderId is required.");
        }

        if (!Ulid.TryParse(orderId, out var parsedOrder))
        {
            return BadRequest("Invalid order id format.");
        }

        var targetOrderId = parsedOrder.ToString();

        // Verify order exists and user has access
        var customerIdForOrder = await mediator.Send(
            new GetOrderCustomerIdQuery(targetOrderId), cancellationToken);

        if (customerIdForOrder is null)
        {
            return NotFound();
        }

        if (!await currentUserAccess.HasCustomerAccessAsync(customerIdForOrder, cancellationToken))
        {
            return Forbid();
        }

        var result = await mediator.Send(
            new GetNotificationsByOrderQuery(UlidId.FromUlid(parsedOrder)), cancellationToken);

        return Ok(result);
    }

    /// <summary>
    ///     Retries a failed notification.
    /// </summary>
    [HttpPost("{notificationId}/retry")]
    [Authorize(Policy = AuthorizationPolicies.RequireOps)]
    public async Task<ActionResult> RetryAsync(
        string notificationId,
        CancellationToken cancellationToken
    )
    {
        if (!Ulid.TryParse(notificationId, out var parsedNotification))
        {
            return BadRequest("Invalid notification id format.");
        }

        var targetNotificationId = parsedNotification.ToString();

        // Look up the notification to verify it exists and get customer ID for access control
        var notificationResult = await mediator.Send(
            new GetNotificationByIdQuery(targetNotificationId), cancellationToken);

        if (!notificationResult.IsSuccess)
        {
            return NotFound(notificationResult.Error);
        }

        var notification = notificationResult.Value;

        if (!await currentUserAccess.HasCustomerAccessAsync(notification.CustomerId.ToString(), cancellationToken))
        {
            return Forbid();
        }

        // Use ProcessNotificationCommand which already handles retry of failed notifications
        var command = new ProcessNotificationCommand(UlidId.FromUlid(parsedNotification));
        var result = await mediator.Send(command, cancellationToken);

        if (!result.IsSuccess)
        {
            return BadRequest(result.Error);
        }

        return NoContent();
    }
}
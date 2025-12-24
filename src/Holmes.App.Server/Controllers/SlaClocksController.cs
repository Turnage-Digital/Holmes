using Holmes.App.Infrastructure.Security;
using Holmes.Core.Domain.ValueObjects;
using Holmes.SlaClocks.Application.Abstractions.Dtos;
using Holmes.SlaClocks.Application.Commands;
using Holmes.SlaClocks.Application.Queries;
using Holmes.Orders.Application.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Holmes.App.Server.Controllers;

[ApiController]
[Authorize]
[Route("api/clocks/sla")]
public sealed class SlaClocksController(
    IMediator mediator,
    ICurrentUserAccess currentUserAccess
) : ControllerBase
{
    /// <summary>
    ///     Gets all SLA clocks for a specific order.
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<SlaClockDto>>> GetByOrderIdAsync(
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
            new GetSlaClocksByOrderIdQuery(targetOrderId), cancellationToken);

        if (!result.IsSuccess)
        {
            return Problem(result.Error);
        }

        return Ok(result.Value);
    }

    /// <summary>
    ///     Pauses an SLA clock with a reason.
    /// </summary>
    [HttpPost("{clockId}/pause")]
    [Authorize(Policy = AuthorizationPolicies.RequireOps)]
    public async Task<ActionResult> PauseAsync(
        string clockId,
        [FromBody] PauseClockRequest request,
        CancellationToken cancellationToken
    )
    {
        if (!Ulid.TryParse(clockId, out var parsedClock))
        {
            return BadRequest("Invalid clock id format.");
        }

        if (string.IsNullOrWhiteSpace(request.Reason))
        {
            return BadRequest("Reason is required.");
        }

        var targetClockId = parsedClock.ToString();

        // Look up the clock to verify it exists and get customer ID for access control
        var clockResult = await mediator.Send(
            new GetSlaClockByIdQuery(targetClockId), cancellationToken);

        if (!clockResult.IsSuccess)
        {
            return NotFound(clockResult.Error);
        }

        var clock = clockResult.Value;

        if (!await currentUserAccess.HasCustomerAccessAsync(clock.CustomerId.ToString(), cancellationToken))
        {
            return Forbid();
        }

        var command = new PauseSlaClockCommand(
            UlidId.FromUlid(parsedClock),
            request.Reason,
            DateTimeOffset.UtcNow
        );

        var result = await mediator.Send(command, cancellationToken);

        if (!result.IsSuccess)
        {
            return BadRequest(result.Error);
        }

        return NoContent();
    }

    /// <summary>
    ///     Resumes a paused SLA clock.
    /// </summary>
    [HttpPost("{clockId}/resume")]
    [Authorize(Policy = AuthorizationPolicies.RequireOps)]
    public async Task<ActionResult> ResumeAsync(
        string clockId,
        CancellationToken cancellationToken
    )
    {
        if (!Ulid.TryParse(clockId, out var parsedClock))
        {
            return BadRequest("Invalid clock id format.");
        }

        var targetClockId = parsedClock.ToString();

        // Look up the clock to verify it exists and get customer ID for access control
        var clockResult = await mediator.Send(
            new GetSlaClockByIdQuery(targetClockId), cancellationToken);

        if (!clockResult.IsSuccess)
        {
            return NotFound(clockResult.Error);
        }

        var clock = clockResult.Value;

        if (!await currentUserAccess.HasCustomerAccessAsync(clock.CustomerId.ToString(), cancellationToken))
        {
            return Forbid();
        }

        var command = new ResumeSlaClockCommand(
            UlidId.FromUlid(parsedClock),
            DateTimeOffset.UtcNow
        );

        var result = await mediator.Send(command, cancellationToken);

        if (!result.IsSuccess)
        {
            return BadRequest(result.Error);
        }

        return NoContent();
    }

    public sealed record PauseClockRequest(string Reason);
}
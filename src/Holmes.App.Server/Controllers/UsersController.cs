using Holmes.App.Infrastructure.Identity;
using Holmes.App.Infrastructure.Identity.Models;
using Holmes.App.Infrastructure.Security;
using Holmes.App.Server.Contracts;
using Holmes.Core.Domain.ValueObjects;
using Holmes.Users.Application.Abstractions.Dtos;
using Holmes.Users.Application.Abstractions.Queries;
using Holmes.Users.Application.Commands;
using Holmes.Users.Domain;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Holmes.App.Server.Controllers;

[ApiController]
[Authorize]
[Route("api/users")]
public sealed class UsersController(
    IMediator mediator,
    IUserQueries userQueries,
    ICurrentUserAccess currentUserAccess,
    IIdentityProvisioningClient identityProvisioningClient
) : ControllerBase
{
    [HttpGet]
    [Authorize(Policy = AuthorizationPolicies.RequireGlobalAdmin)]
    public async Task<ActionResult<PaginatedResponse<UserDto>>> GetUsers(
        [FromQuery] PaginationQuery query,
        CancellationToken cancellationToken
    )
    {
        var (page, pageSize) = PaginationNormalization.Normalize(query);
        var result = await userQueries.GetUsersPagedAsync(page, pageSize, cancellationToken);

        return Ok(PaginatedResponse<UserDto>.Create(result.Items, page, pageSize, result.TotalCount));
    }

    [HttpGet("me")]
    public async Task<ActionResult<CurrentUserDto>> GetMe(CancellationToken cancellationToken)
    {
        var currentUserId = await currentUserAccess.GetUserIdAsync(cancellationToken);
        var currentUser = await userQueries.GetCurrentUserAsync(currentUserId.ToString(), cancellationToken);

        if (currentUser is null)
        {
            return NotFound();
        }

        return Ok(currentUser);
    }

    [HttpPost("invitations")]
    [Authorize(Policy = AuthorizationPolicies.RequireGlobalAdmin)]
    public async Task<ActionResult<InviteUserResponse>> InviteUser(
        [FromBody] InviteUserRequest request,
        CancellationToken cancellationToken
    )
    {
        IReadOnlyCollection<InviteUserRole> roles = (request.Roles?.Count ?? 0) == 0
            ? [new InviteUserRole(UserRole.Admin, null)]
            : request.Roles!.Select(r => new InviteUserRole(r.Role, r.CustomerId)).ToList();

        var command = new InviteUserCommand(
            request.Email,
            request.DisplayName,
            roles,
            DateTimeOffset.UtcNow);

        var result = await mediator.Send(command, cancellationToken);
        if (!result.IsSuccess)
        {
            return BadRequest(result.Error);
        }

        var invitedUserId = result.Value;
        var mappedUser = await userQueries.GetByIdAsync(invitedUserId, cancellationToken);

        if (mappedUser is null)
        {
            return Problem("Failed to load invited user.");
        }

        var provisioning = await identityProvisioningClient.ProvisionUserAsync(
            new ProvisionIdentityUserRequest(invitedUserId.ToString(), mappedUser.Email, mappedUser.DisplayName),
            cancellationToken);

        return Created(string.Empty, new InviteUserResponse(mappedUser, provisioning.ConfirmationLink));
    }

    [HttpPost("{userId}/roles")]
    [Authorize(Policy = AuthorizationPolicies.RequireGlobalAdmin)]
    public async Task<IActionResult> GrantRole(
        string userId,
        [FromBody] ModifyUserRoleRequest request,
        CancellationToken cancellationToken
    )
    {
        if (!Ulid.TryParse(userId, out var parsedTarget))
        {
            return BadRequest("Invalid user id format.");
        }

        var result = await mediator.Send(new GrantUserRoleCommand(
            UlidId.FromUlid(parsedTarget),
            request.Role,
            request.CustomerId,
            DateTimeOffset.UtcNow), cancellationToken);

        return result.IsSuccess ? NoContent() : BadRequest(result.Error);
    }

    [HttpDelete("{userId}/roles")]
    [Authorize(Policy = AuthorizationPolicies.RequireGlobalAdmin)]
    public async Task<IActionResult> RevokeRole(
        string userId,
        [FromBody] ModifyUserRoleRequest request,
        CancellationToken cancellationToken
    )
    {
        if (!Ulid.TryParse(userId, out var parsedTarget))
        {
            return BadRequest("Invalid user id format.");
        }

        var result = await mediator.Send(new RevokeUserRoleCommand(
            UlidId.FromUlid(parsedTarget),
            request.Role,
            request.CustomerId,
            DateTimeOffset.UtcNow), cancellationToken);

        return result.IsSuccess ? NoContent() : BadRequest(result.Error);
    }

    public sealed record InviteUserRequest(
        string Email,
        string? DisplayName,
        bool? SendInviteEmail,
        IReadOnlyCollection<InviteUserRoleRequest>? Roles
    );

    public sealed record InviteUserRoleRequest(UserRole Role, string? CustomerId);

    public sealed record ModifyUserRoleRequest(UserRole Role, string? CustomerId);

    public sealed record InviteUserResponse(UserDto User, string ConfirmationLink);
}
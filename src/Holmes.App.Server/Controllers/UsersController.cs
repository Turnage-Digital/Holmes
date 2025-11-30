using Holmes.App.Server.Contracts;
using Holmes.App.Server.Identity;
using Holmes.App.Server.Identity.Models;
using Holmes.App.Server.Mappers;
using Holmes.App.Server.Security;
using Holmes.Core.Application;
using Holmes.Core.Application.Abstractions.Specifications;
using Holmes.Core.Domain.ValueObjects;
using Holmes.Users.Application.Abstractions.Dtos;
using Holmes.Users.Application.Commands;
using Holmes.Users.Domain;
using Holmes.Users.Infrastructure.Sql;
using Holmes.Users.Infrastructure.Sql.Entities;
using Holmes.Users.Infrastructure.Sql.Specifications;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Holmes.App.Server.Controllers;

[ApiController]
[Authorize]
[Route("api/users")]
public class UsersController(
    IMediator mediator,
    UsersDbContext dbContext,
    ICurrentUserAccess currentUserAccess,
    ISpecificationQueryExecutor specificationExecutor,
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
        var countSpec = new UsersWithDetailsSpecification();
        var totalItems = await specificationExecutor
            .Apply(dbContext.Users.AsNoTracking(), countSpec)
            .CountAsync(cancellationToken);

        var usersSpec = new UsersWithDetailsSpecification(page, pageSize);
        var users = await specificationExecutor
            .Apply(dbContext.Users.AsNoTracking(), usersSpec)
            .ToListAsync(cancellationToken);

        var userIds = users.Select(u => u.UserId).ToList();

        var directorySpec = new UserDirectoryByIdsSpecification(userIds);
        var directoryEntries = await specificationExecutor
            .Apply(dbContext.UserDirectory.AsNoTracking(), directorySpec)
            .ToDictionaryAsync(x => x.UserId, cancellationToken);

        var items = users
            .Select(u => UserDtoMapper.ToDto(u, directoryEntries.GetValueOrDefault(u.UserId)))
            .ToList();

        return Ok(PaginatedResponse<UserDto>.Create(items, page, pageSize, totalItems));
    }

    [HttpGet("me")]
    public async Task<ActionResult<CurrentUserDto>> GetMe(CancellationToken cancellationToken)
    {
        var currentUserId = await currentUserAccess.GetUserIdAsync(cancellationToken);
        var directorySpec = new UserDirectoryByIdsSpecification([currentUserId.ToString()]);
        var projection = await specificationExecutor
            .Apply(dbContext.UserDirectory.AsNoTracking(), directorySpec)
            .SingleOrDefaultAsync(cancellationToken);

        if (projection is null)
        {
            return NotFound();
        }

        var roles = await dbContext.UserRoleMemberships
            .AsNoTracking()
            .Where(x => x.UserId == currentUserId.ToString())
            .Select(x => new UserRoleDto(x.Role, x.CustomerId))
            .ToListAsync(cancellationToken);

        return Ok(new CurrentUserDto(
            currentUserId.ToString(),
            projection.Email,
            projection.DisplayName,
            projection.Issuer,
            projection.Subject,
            projection.Status,
            projection.LastAuthenticatedAt,
            roles));
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

        var invitedUserId = result.Value.ToString();
        var userSpec = new UserWithDetailsByIdSpecification(invitedUserId);
        var user = await specificationExecutor
            .Apply(dbContext.Users.AsNoTracking(), userSpec)
            .SingleAsync(cancellationToken);

        var directorySpec = new UserDirectoryByIdsSpecification([invitedUserId]);
        var directory = await specificationExecutor
            .Apply(dbContext.UserDirectory.AsNoTracking(), directorySpec)
            .SingleAsync(cancellationToken);

        var mappedUser = UserDtoMapper.ToDto(user, directory);

        var provisioning = await identityProvisioningClient.ProvisionUserAsync(
            new ProvisionIdentityUserRequest(invitedUserId, mappedUser.Email, mappedUser.DisplayName),
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

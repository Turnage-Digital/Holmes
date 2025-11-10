using Holmes.App.Server.Contracts;
using Holmes.Core.Application;
using Holmes.Core.Domain.ValueObjects;
using Holmes.Users.Application.Commands;
using Holmes.Users.Domain;
using Holmes.Users.Infrastructure.Sql;
using Holmes.Users.Infrastructure.Sql.Entities;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace Holmes.App.Server.Controllers;

[ApiController]
[Authorize]
[Route("api/users")]
public class UsersController(
    IMediator mediator,
    UsersDbContext dbContext,
    ICurrentUserInitializer currentUserInitializer
) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PaginatedResponse<UserListItemResponse>>> GetUsers(
        [FromQuery] PaginationQuery query,
        CancellationToken cancellationToken
    )
    {
        var actor = await GetCurrentUserAsync(cancellationToken);
        if (!await IsGlobalAdminAsync(actor, cancellationToken))
        {
            return Forbid();
        }

        var (page, pageSize) = NormalizePagination(query);

        var baseQuery = dbContext.Users
            .AsNoTracking()
            .Include(x => x.ExternalIdentities)
            .Include(x => x.RoleMemberships)
            .OrderBy(x => x.Email);

        var totalItems = await baseQuery.CountAsync(cancellationToken);
        var users = await baseQuery
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var userIds = users.Select(u => u.UserId).ToList();

        var directoryEntries = await dbContext.UserDirectory
            .AsNoTracking()
            .Where(x => userIds.Contains(x.UserId))
            .ToDictionaryAsync(x => x.UserId, cancellationToken);

        var items = users
            .Select(u => MapUser(u, directoryEntries.GetValueOrDefault(u.UserId)))
            .ToList();

        return Ok(PaginatedResponse<UserListItemResponse>.Create(items, page, pageSize, totalItems));
    }

    [HttpGet("me")]
    public async Task<ActionResult<UserResponse>> GetMe(CancellationToken cancellationToken)
    {
        var currentUserId = await GetCurrentUserAsync(cancellationToken);
        var projection = await dbContext.UserDirectory
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.UserId == currentUserId.ToString(), cancellationToken);

        if (projection is null)
        {
            return NotFound();
        }

        var roles = await dbContext.UserRoleMemberships
            .AsNoTracking()
            .Where(x => x.UserId == currentUserId.ToString())
            .Select(x => new UserRoleResponse(x.Role, x.CustomerId))
            .ToListAsync(cancellationToken);

        return Ok(new UserResponse(
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
    public async Task<ActionResult<UserListItemResponse>> InviteUser(
        [FromBody] InviteUserRequest request,
        CancellationToken cancellationToken
    )
    {
        var actor = await GetCurrentUserAsync(cancellationToken);
        if (!await IsGlobalAdminAsync(actor, cancellationToken))
        {
            return Forbid();
        }

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
        var user = await dbContext.Users
            .AsNoTracking()
            .Include(x => x.ExternalIdentities)
            .Include(x => x.RoleMemberships)
            .SingleAsync(x => x.UserId == invitedUserId, cancellationToken);

        var directory = await dbContext.UserDirectory
            .AsNoTracking()
            .SingleAsync(x => x.UserId == invitedUserId, cancellationToken);

        return Created(string.Empty, MapUser(user, directory));
    }

    [HttpPost("{userId}/roles")]
    public async Task<IActionResult> GrantRole(
        string userId,
        [FromBody] ModifyUserRoleRequest request,
        CancellationToken cancellationToken
    )
    {
        var actorId = await GetCurrentUserAsync(cancellationToken);
        if (!await IsGlobalAdminAsync(actorId, cancellationToken))
        {
            return Forbid();
        }

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
    public async Task<IActionResult> RevokeRole(
        string userId,
        [FromBody] ModifyUserRoleRequest request,
        CancellationToken cancellationToken
    )
    {
        var actorId = await GetCurrentUserAsync(cancellationToken);
        if (!await IsGlobalAdminAsync(actorId, cancellationToken))
        {
            return Forbid();
        }

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

    private async Task<UlidId> GetCurrentUserAsync(CancellationToken cancellationToken)
    {
        return await currentUserInitializer.EnsureCurrentUserIdAsync(cancellationToken);
    }

    private Task<bool> IsGlobalAdminAsync(UlidId userId, CancellationToken cancellationToken)
    {
        return dbContext.UserRoleMemberships
            .AsNoTracking()
            .AnyAsync(x =>
                    x.UserId == userId.ToString() &&
                    x.Role == UserRole.Admin &&
                    x.CustomerId == null,
                cancellationToken);
    }

    private static (int Page, int PageSize) NormalizePagination(PaginationQuery query)
    {
        var page = query.Page <= 0 ? 1 : query.Page;
        var size = query.PageSize <= 0 ? 25 : Math.Min(query.PageSize, 100);
        return (page, size);
    }

    private static UserListItemResponse MapUser(UserDb user, UserDirectoryDb? directory)
    {
        var primaryIdentity = user.ExternalIdentities
            .OrderByDescending(x => x.LastSeenAt)
            .FirstOrDefault();

        var identity = primaryIdentity is null
            ? null
            : new ExternalIdentityResponse(
                primaryIdentity.Issuer,
                primaryIdentity.Subject,
                primaryIdentity.AuthenticationMethod,
                primaryIdentity.LinkedAt,
                primaryIdentity.LastSeenAt);

        var roles = user.RoleMemberships
            .OrderByDescending(r => r.GrantedAt)
            .Select(r => new RoleAssignmentResponse(
                r.Id.ToString(),
                r.Role,
                r.CustomerId,
                r.GrantedBy.ToString(),
                r.GrantedAt))
            .ToList();

        var lastSeen = directory?.LastAuthenticatedAt ?? user.CreatedAt;

        return new UserListItemResponse(
            user.UserId,
            user.Email,
            user.DisplayName,
            user.Status,
            lastSeen,
            user.CreatedAt,
            user.CreatedAt,
            roles,
            identity);
    }

    public sealed record InviteUserRequest(
        string Email,
        string? DisplayName,
        bool? SendInviteEmail,
        IReadOnlyCollection<InviteUserRoleRequest>? Roles
    );

    public sealed record InviteUserRoleRequest(UserRole Role, string? CustomerId);

    public sealed record RoleAssignmentResponse(
        string Id,
        UserRole Role,
        string? CustomerId,
        string GrantedBy,
        DateTimeOffset GrantedAt
    );

    public sealed record ExternalIdentityResponse(
        string Issuer,
        string Subject,
        string? AuthenticationMethod,
        DateTimeOffset LinkedAt,
        DateTimeOffset LastSeenAt
    );

    public sealed record UserListItemResponse(
        string Id,
        string Email,
        string? DisplayName,
        UserStatus Status,
        DateTimeOffset LastSeenAt,
        DateTimeOffset CreatedAt,
        DateTimeOffset UpdatedAt,
        IReadOnlyCollection<RoleAssignmentResponse> RoleAssignments,
        ExternalIdentityResponse? ExternalIdentity
    );

    public sealed record ModifyUserRoleRequest(UserRole Role, string? CustomerId);

    public sealed record UserRoleResponse(UserRole Role, string? CustomerId);

    public sealed record UserResponse(
        string UserId,
        string Email,
        string? DisplayName,
        string Issuer,
        string Subject,
        UserStatus Status,
        DateTimeOffset LastAuthenticatedAt,
        IReadOnlyCollection<UserRoleResponse> Roles
    );
}

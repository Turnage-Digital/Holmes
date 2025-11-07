using Holmes.App.Server.Security;
using Holmes.Core.Domain.ValueObjects;
using Holmes.Users.Application.Commands;
using Holmes.Users.Domain;
using Holmes.Users.Infrastructure.Sql;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Holmes.App.Server.Controllers;

[ApiController]
[Authorize]
[Route("users")]
public class UsersController : ControllerBase
{
    private readonly UsersDbContext _dbContext;
    private readonly IMediator _mediator;
    private readonly IUserContext _userContext;

    public UsersController(
        IMediator mediator,
        UsersDbContext dbContext,
        IUserContext userContext
    )
    {
        _mediator = mediator;
        _dbContext = dbContext;
        _userContext = userContext;
    }

    [HttpGet("me")]
    public async Task<ActionResult<UserResponse>> GetMe(CancellationToken cancellationToken)
    {
        var currentUserId = await EnsureCurrentUserAsync(cancellationToken);
        var projection = await _dbContext.UserDirectory
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.UserId == currentUserId.ToString(), cancellationToken);

        if (projection is null)
        {
            return NotFound();
        }

        var roles = await _dbContext.UserRoleMemberships
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

    [HttpPost("{userId}/roles")]
    public async Task<IActionResult> GrantRole(
        string userId,
        [FromBody] ModifyUserRoleRequest request,
        CancellationToken cancellationToken
    )
    {
        var actorId = await EnsureCurrentUserAsync(cancellationToken);
        if (!await IsGlobalAdminAsync(actorId, cancellationToken))
        {
            return Forbid();
        }

        if (!Ulid.TryParse(userId, out var parsedTarget))
        {
            return BadRequest("Invalid user id format.");
        }

        var result = await _mediator.Send(new GrantUserRoleCommand(
            UlidId.FromUlid(parsedTarget),
            request.Role,
            request.CustomerId,
            actorId,
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
        var actorId = await EnsureCurrentUserAsync(cancellationToken);
        if (!await IsGlobalAdminAsync(actorId, cancellationToken))
        {
            return Forbid();
        }

        if (!Ulid.TryParse(userId, out var parsedTarget))
        {
            return BadRequest("Invalid user id format.");
        }

        var result = await _mediator.Send(new RevokeUserRoleCommand(
            UlidId.FromUlid(parsedTarget),
            request.Role,
            request.CustomerId,
            actorId,
            DateTimeOffset.UtcNow), cancellationToken);

        return result.IsSuccess ? NoContent() : BadRequest(result.Error);
    }

    private async Task<UlidId> EnsureCurrentUserAsync(CancellationToken cancellationToken)
    {
        var command = new RegisterExternalUserCommand(
            _userContext.Issuer,
            _userContext.Subject,
            _userContext.Email,
            _userContext.DisplayName,
            _userContext.AuthenticationMethod,
            DateTimeOffset.UtcNow);

        return await _mediator.Send(command, cancellationToken);
    }

    private Task<bool> IsGlobalAdminAsync(UlidId userId, CancellationToken cancellationToken)
    {
        return _dbContext.UserRoleMemberships
            .AsNoTracking()
            .AnyAsync(x =>
                    x.UserId == userId.ToString() &&
                    x.Role == UserRole.Admin &&
                    x.CustomerId == null,
                cancellationToken);
    }

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
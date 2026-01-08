using Holmes.Core.Application;
using Holmes.Core.Contracts;
using Holmes.Core.Domain.ValueObjects;
using Holmes.Users.Contracts;
using Holmes.Users.Contracts.Dtos;
using Holmes.Users.Domain;
using MediatR;

namespace Holmes.Users.Application.Commands;

public sealed record InviteUserCommand(
    string Email,
    string? DisplayName,
    IReadOnlyCollection<InviteUserRole> Roles,
    DateTimeOffset InvitedAt
) : RequestBase<Result<InviteUserResultDto>>;

public sealed record InviteUserRole(UserRole Role, string? CustomerId);

public sealed class InviteUserCommandHandler(
    IUsersUnitOfWork unitOfWork,
    IUserQueries userQueries,
    IIdentityProvisioningService provisioningService
) : IRequestHandler<InviteUserCommand, Result<InviteUserResultDto>>
{
    public async Task<Result<InviteUserResultDto>> Handle(
        InviteUserCommand request,
        CancellationToken cancellationToken
    )
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(request.Email);

        var normalizedEmail = request.Email.Trim();

        // Check existence via query interface (query side)
        var existing = await userQueries.GetByEmailAsync(normalizedEmail, cancellationToken);
        if (existing is not null)
        {
            return Result.Fail<InviteUserResultDto>($"User with email '{normalizedEmail}' already exists.");
        }

        var roles = request.Roles.Count == 0
            ? [new InviteUserRole(UserRole.Operations, null)]
            : request.Roles;

        var user = User.Invite(UlidId.NewUlid(), normalizedEmail, request.DisplayName?.Trim(), request.InvitedAt);
        var actor = request.GetUserUlid();
        foreach (var role in roles)
        {
            user.GrantRole(role.Role, role.CustomerId, actor, request.InvitedAt);
        }

        await unitOfWork.Users.AddAsync(user, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        var created = await userQueries.GetByIdAsync(user.Id, cancellationToken);
        if (created is null)
        {
            return Result.Fail<InviteUserResultDto>("Failed to load invited user.");
        }

        var provisioning = await provisioningService.ProvisionUserAsync(
            user.Id.ToString(),
            created.Email,
            created.DisplayName,
            cancellationToken);

        return Result.Success(new InviteUserResultDto(created, provisioning.ConfirmationLink));
    }
}
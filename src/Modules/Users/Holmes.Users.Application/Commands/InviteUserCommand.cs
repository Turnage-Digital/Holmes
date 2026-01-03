using Holmes.Core.Contracts;
using Holmes.Core.Application;
using Holmes.Core.Domain.ValueObjects;
using Holmes.Users.Contracts;
using Holmes.Users.Domain;
using MediatR;

namespace Holmes.Users.Application.Commands;

public sealed record InviteUserCommand(
    string Email,
    string? DisplayName,
    IReadOnlyCollection<InviteUserRole> Roles,
    DateTimeOffset InvitedAt
) : RequestBase<Result<UlidId>>;

public sealed record InviteUserRole(UserRole Role, string? CustomerId);

public sealed class InviteUserCommandHandler(IUsersUnitOfWork unitOfWork, IUserQueries userQueries)
    : IRequestHandler<InviteUserCommand, Result<UlidId>>
{
    public async Task<Result<UlidId>> Handle(InviteUserCommand request, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(request.Email);

        var normalizedEmail = request.Email.Trim();

        // Check existence via query interface (query side)
        var existing = await userQueries.GetByEmailAsync(normalizedEmail, cancellationToken);
        if (existing is not null)
        {
            return Result.Fail<UlidId>($"User with email '{normalizedEmail}' already exists.");
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
        return Result.Success(user.Id);
    }
}
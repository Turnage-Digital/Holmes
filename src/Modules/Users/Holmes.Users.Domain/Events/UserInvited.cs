using Holmes.Core.Domain.ValueObjects;
using MediatR;

namespace Holmes.Users.Domain.Events;

public sealed record UserInvited(
    UlidId UserId,
    string Email,
    string? DisplayName,
    DateTimeOffset InvitedAt
) : INotification;
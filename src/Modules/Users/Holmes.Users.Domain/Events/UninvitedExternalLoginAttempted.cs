using MediatR;

namespace Holmes.Users.Domain.Events;

public sealed record UninvitedExternalLoginAttempted(
    string Issuer,
    string Subject,
    string Email,
    DateTimeOffset AttemptedAt
) : INotification;
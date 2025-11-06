using System;
using Holmes.Core.Domain.ValueObjects;
using MediatR;

namespace Holmes.Users.Domain.Users.Events;

public sealed record UserRegistered(
    UlidId UserId,
    string Issuer,
    string Subject,
    string Email,
    string? DisplayName,
    string? AuthenticationMethod,
    DateTimeOffset RegisteredAt
) : INotification;

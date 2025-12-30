using Holmes.Core.Contracts;
using Holmes.Core.Domain;
using Holmes.Core.Domain.ValueObjects;

namespace Holmes.Services.Application.Commands;

public sealed record RetryServiceCommand(
    UlidId ServiceId,
    DateTimeOffset RetriedAt
) : RequestBase<Result>;
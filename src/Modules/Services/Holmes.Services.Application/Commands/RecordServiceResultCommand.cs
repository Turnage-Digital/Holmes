using Holmes.Core.Contracts;
using Holmes.Core.Domain;
using Holmes.Core.Domain.ValueObjects;
using Holmes.Services.Domain;

namespace Holmes.Services.Application.Commands;

public sealed record RecordServiceResultCommand(
    UlidId ServiceId,
    ServiceResult Result,
    DateTimeOffset CompletedAt
) : RequestBase<Result>;
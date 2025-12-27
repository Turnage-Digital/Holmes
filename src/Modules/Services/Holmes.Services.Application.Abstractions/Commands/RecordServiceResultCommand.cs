using Holmes.Core.Application;
using Holmes.Core.Domain;
using Holmes.Core.Domain.ValueObjects;
using Holmes.Services.Domain;

namespace Holmes.Services.Application.Abstractions.Commands;

public sealed record RecordServiceResultCommand(
    UlidId ServiceId,
    ServiceResult Result,
    DateTimeOffset CompletedAt
) : RequestBase<Result>;
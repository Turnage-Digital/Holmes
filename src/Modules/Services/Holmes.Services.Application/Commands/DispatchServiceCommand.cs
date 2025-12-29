using Holmes.Core.Application.Abstractions;
using Holmes.Core.Domain;
using Holmes.Core.Domain.ValueObjects;

namespace Holmes.Services.Application.Commands;

public sealed record DispatchServiceCommand(
    UlidId ServiceId,
    DateTimeOffset DispatchedAt
) : RequestBase<Result>;
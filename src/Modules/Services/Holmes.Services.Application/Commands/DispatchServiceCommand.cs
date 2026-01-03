using Holmes.Core.Application;
using Holmes.Core.Contracts;
using Holmes.Core.Domain.ValueObjects;

namespace Holmes.Services.Application.Commands;

public sealed record DispatchServiceCommand(
    UlidId ServiceId,
    DateTimeOffset DispatchedAt
) : RequestBase<Result>;
using Holmes.Core.Contracts;
using Holmes.Core.Domain;
using Holmes.Core.Domain.ValueObjects;

namespace Holmes.Orders.Application.Commands;

public sealed record BlockOrderCommand(
    UlidId OrderId,
    string Reason,
    DateTimeOffset BlockedAt
) : RequestBase<Result>;
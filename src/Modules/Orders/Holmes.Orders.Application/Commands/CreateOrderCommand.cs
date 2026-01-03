using Holmes.Core.Contracts;
using Holmes.Core.Application;
using Holmes.Core.Domain.ValueObjects;

namespace Holmes.Orders.Application.Commands;

public sealed record CreateOrderCommand(
    UlidId OrderId,
    UlidId SubjectId,
    UlidId CustomerId,
    string PolicySnapshotId,
    DateTimeOffset CreatedAt,
    string? PackageCode
) : RequestBase<Result>;
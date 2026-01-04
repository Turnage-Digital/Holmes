using Holmes.Core.Application;
using Holmes.Core.Contracts;
using Holmes.Core.Domain.ValueObjects;
using Holmes.Orders.Contracts.Dtos;

namespace Holmes.Orders.Application.Commands;

public sealed record CreateOrderCommand(
    UlidId CustomerId,
    string PolicySnapshotId,
    string SubjectEmail,
    string? SubjectPhone,
    string? PackageCode,
    DateTimeOffset CreatedAt
) : RequestBase<Result<CreateOrderResponse>>;
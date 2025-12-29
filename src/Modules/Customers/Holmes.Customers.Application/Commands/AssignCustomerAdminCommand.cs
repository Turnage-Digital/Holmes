using Holmes.Core.Application.Abstractions;
using Holmes.Core.Domain;
using Holmes.Core.Domain.ValueObjects;

namespace Holmes.Customers.Application.Commands;

public sealed record AssignCustomerAdminCommand(
    UlidId TargetCustomerId,
    UlidId TargetUserId,
    DateTimeOffset AssignedAt
) : RequestBase<Result>;
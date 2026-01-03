using Holmes.Core.Contracts;
using Holmes.Core.Application;
using Holmes.Core.Domain.ValueObjects;

namespace Holmes.Customers.Application.Commands;

public sealed record AssignCustomerAdminCommand(
    UlidId TargetCustomerId,
    UlidId TargetUserId,
    DateTimeOffset AssignedAt
) : RequestBase<Result>;
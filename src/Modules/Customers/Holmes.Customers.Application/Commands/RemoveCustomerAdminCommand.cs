using Holmes.Core.Contracts;
using Holmes.Core.Domain;
using Holmes.Core.Domain.ValueObjects;

namespace Holmes.Customers.Application.Commands;

public sealed record RemoveCustomerAdminCommand(
    UlidId TargetCustomerId,
    UlidId TargetUserId,
    DateTimeOffset RemovedAt
) : RequestBase<Result>;
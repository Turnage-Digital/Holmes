using Holmes.Core.Contracts;
using Holmes.Core.Domain;
using Holmes.Core.Domain.ValueObjects;

namespace Holmes.Customers.Application.Commands;

public sealed record SuspendCustomerCommand(
    UlidId TargetCustomerId,
    string Reason,
    DateTimeOffset SuspendedAt
) : RequestBase<Result>;
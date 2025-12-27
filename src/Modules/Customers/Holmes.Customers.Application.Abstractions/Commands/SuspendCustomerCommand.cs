using Holmes.Core.Application;
using Holmes.Core.Domain;
using Holmes.Core.Domain.ValueObjects;

namespace Holmes.Customers.Application.Abstractions.Commands;

public sealed record SuspendCustomerCommand(
    UlidId TargetCustomerId,
    string Reason,
    DateTimeOffset SuspendedAt
) : RequestBase<Result>;
using Holmes.Core.Application;
using Holmes.Core.Contracts;
using Holmes.Core.Domain.ValueObjects;

namespace Holmes.Customers.Application.Commands;

public sealed record RenameCustomerCommand(
    UlidId TargetCustomerId,
    string Name,
    DateTimeOffset RenamedAt
) : RequestBase<Result>;
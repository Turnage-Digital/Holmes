using Holmes.Core.Application;
using Holmes.Core.Domain;
using Holmes.Core.Domain.ValueObjects;

namespace Holmes.Customers.Application.Abstractions.Commands;

public sealed record RenameCustomerCommand(
    UlidId TargetCustomerId,
    string Name,
    DateTimeOffset RenamedAt
) : RequestBase<Result>;

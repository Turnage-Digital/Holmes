using Holmes.Core.Application.Abstractions;
using Holmes.Core.Domain;
using Holmes.Core.Domain.ValueObjects;

namespace Holmes.Customers.Application.Commands;

public sealed record RenameCustomerCommand(
    UlidId TargetCustomerId,
    string Name,
    DateTimeOffset RenamedAt
) : RequestBase<Result>;
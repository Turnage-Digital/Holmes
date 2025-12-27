using Holmes.Core.Application;
using Holmes.Core.Domain.ValueObjects;

namespace Holmes.Customers.Application.Abstractions.Commands;

public sealed record RegisterCustomerCommand(
    string Name,
    DateTimeOffset RegisteredAt
) : RequestBase<UlidId>;

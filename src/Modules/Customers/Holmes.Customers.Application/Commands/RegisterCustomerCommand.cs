using Holmes.Core.Application.Abstractions;
using Holmes.Core.Domain.ValueObjects;

namespace Holmes.Customers.Application.Commands;

public sealed record RegisterCustomerCommand(
    string Name,
    DateTimeOffset RegisteredAt
) : RequestBase<UlidId>;
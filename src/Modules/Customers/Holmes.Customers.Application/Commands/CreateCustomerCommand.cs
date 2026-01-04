using Holmes.Core.Application;
using Holmes.Core.Contracts;
using Holmes.Customers.Contracts.Dtos;

namespace Holmes.Customers.Application.Commands;

public sealed record CreateCustomerCommand(
    string Name,
    string? PolicySnapshotId,
    string? BillingEmail,
    IReadOnlyCollection<CreateContactInfo>? Contacts,
    DateTimeOffset CreatedAt
) : RequestBase<Result<CustomerListItemDto>>;
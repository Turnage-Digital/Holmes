using Holmes.Core.Domain;
using Holmes.Core.Domain.ValueObjects;
using MediatR;

namespace Holmes.Customers.Application.Commands;

/// <summary>
///     Contact information for customer profile creation.
/// </summary>
public sealed record CreateContactInfo(
    string Name,
    string Email,
    string? Phone,
    string? Role
);

/// <summary>
///     Creates a customer profile with optional contacts.
/// </summary>
public sealed record CreateCustomerProfileCommand(
    UlidId CustomerId,
    string? PolicySnapshotId,
    string? BillingEmail,
    IReadOnlyCollection<CreateContactInfo>? Contacts,
    DateTimeOffset CreatedAt
) : IRequest<Result>;
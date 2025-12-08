using Holmes.Core.Domain.Results;
using Holmes.Core.Domain.ValueObjects;
using Holmes.Customers.Domain;
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

public sealed class CreateCustomerProfileCommandHandler(
    ICustomerProfileRepository profileRepository
) : IRequestHandler<CreateCustomerProfileCommand, Result>
{
    public async Task<Result> Handle(CreateCustomerProfileCommand request, CancellationToken cancellationToken)
    {
        var contacts = request.Contacts?
            .Select(c => new CustomerContactInfo(c.Name, c.Email, c.Phone, c.Role))
            .ToList();

        await profileRepository.CreateProfileAsync(
            request.CustomerId.ToString(),
            request.PolicySnapshotId,
            request.BillingEmail,
            contacts,
            request.CreatedAt,
            cancellationToken);

        return Result.Success();
    }
}
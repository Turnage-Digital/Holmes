using Holmes.Core.Domain;
using Holmes.Customers.Application.Commands;
using Holmes.Customers.Domain;
using MediatR;

namespace Holmes.Customers.Application.Commands;

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
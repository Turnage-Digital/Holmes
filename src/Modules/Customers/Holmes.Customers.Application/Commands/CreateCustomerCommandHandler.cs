using Holmes.Core.Application;
using Holmes.Core.Domain.ValueObjects;
using Holmes.Customers.Contracts;
using Holmes.Customers.Contracts.Dtos;
using Holmes.Customers.Domain;
using Holmes.Users.Contracts;
using MediatR;

namespace Holmes.Customers.Application.Commands;

public sealed class CreateCustomerCommandHandler(
    ICustomersUnitOfWork unitOfWork,
    ICustomerProfileRepository profileRepository,
    ICustomerQueries customerQueries,
    IUserAccessQueries userAccessQueries
) : IRequestHandler<CreateCustomerCommand, Result<CustomerListItemDto>>
{
    public async Task<Result<CustomerListItemDto>> Handle(
        CreateCustomerCommand request,
        CancellationToken cancellationToken
    )
    {
        var actor = request.GetUserUlid();
        var isGlobalAdmin = await userAccessQueries.IsGlobalAdminAsync(actor, cancellationToken);
        if (!isGlobalAdmin)
        {
            return Result.Fail<CustomerListItemDto>(ResultErrors.Forbidden);
        }

        var customerId = UlidId.NewUlid();
        var customer = Customer.Register(customerId, request.Name, request.CreatedAt);
        customer.AssignAdmin(actor, actor, request.CreatedAt);

        await unitOfWork.Customers.AddAsync(customer, cancellationToken);

        var contacts = request.Contacts?
            .Select(c => new CustomerContactInfo(c.Name, c.Email, c.Phone, c.Role))
            .ToList();

        await profileRepository.CreateProfileAsync(
            customerId.ToString(),
            request.PolicySnapshotId,
            request.BillingEmail,
            contacts,
            request.CreatedAt,
            cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        var listItem = await customerQueries.GetListItemByIdAsync(
            customerId.ToString(),
            cancellationToken);

        return listItem is null
            ? Result.Fail<CustomerListItemDto>("Failed to load created customer.")
            : Result.Success(listItem);
    }
}
using Holmes.Core.Application;
using Holmes.Core.Domain.ValueObjects;
using Holmes.Services.Domain;
using MediatR;

namespace Holmes.Services.Application.Commands;

public sealed class CreateServiceCommandHandler(
    IServicesUnitOfWork unitOfWork
) : IRequestHandler<CreateServiceCommand, Result<UlidId>>
{
    public async Task<Result<UlidId>> Handle(
        CreateServiceCommand request,
        CancellationToken cancellationToken
    )
    {
        var serviceType = ServiceType.FromCode(request.ServiceTypeCode);
        if (serviceType is null)
        {
            return Result.Fail<UlidId>($"Unknown service type code: {request.ServiceTypeCode}");
        }

        var service = Service.Create(
            UlidId.NewUlid(),
            request.OrderId,
            request.CustomerId,
            serviceType,
            request.Tier,
            request.Scope,
            request.CatalogSnapshotId,
            request.CreatedAt);

        unitOfWork.Services.Add(service);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(service.Id);
    }
}
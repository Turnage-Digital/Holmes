using Holmes.Core.Application;
using Holmes.Core.Domain;
using Holmes.Core.Domain.ValueObjects;
using Holmes.Services.Domain;
using MediatR;

namespace Holmes.Services.Application.Commands;

public sealed record CreateServiceRequestCommand(
    UlidId OrderId,
    UlidId CustomerId,
    string ServiceTypeCode,
    int Tier,
    ServiceScope? Scope,
    UlidId? CatalogSnapshotId,
    DateTimeOffset CreatedAt
) : RequestBase<Result<UlidId>>;

public sealed class CreateServiceRequestCommandHandler(
    IServicesUnitOfWork unitOfWork
) : IRequestHandler<CreateServiceRequestCommand, Result<UlidId>>
{
    public async Task<Result<UlidId>> Handle(
        CreateServiceRequestCommand request,
        CancellationToken cancellationToken
    )
    {
        var serviceType = ServiceType.FromCode(request.ServiceTypeCode);
        if (serviceType is null)
        {
            return Result.Fail<UlidId>($"Unknown service type code: {request.ServiceTypeCode}");
        }

        var serviceRequest = ServiceRequest.Create(
            UlidId.NewUlid(),
            request.OrderId,
            request.CustomerId,
            serviceType,
            request.Tier,
            request.Scope,
            request.CatalogSnapshotId,
            request.CreatedAt);

        unitOfWork.ServiceRequests.Add(serviceRequest);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(serviceRequest.Id);
    }
}
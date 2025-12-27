using Holmes.Core.Domain;
using Holmes.Services.Application.Abstractions.Commands;
using Holmes.Services.Domain;
using MediatR;

namespace Holmes.Services.Application.Commands;

public sealed class CancelServiceCommandHandler(
    IServicesUnitOfWork unitOfWork
) : IRequestHandler<CancelServiceCommand, Result>
{
    public async Task<Result> Handle(
        CancelServiceCommand request,
        CancellationToken cancellationToken
    )
    {
        var service = await unitOfWork.Services.GetByIdAsync(
            request.ServiceId, cancellationToken);

        if (service is null)
        {
            return Result.Fail($"Service {request.ServiceId} not found");
        }

        if (service.IsTerminal && service.Status != ServiceStatus.Canceled)
        {
            return Result.Fail("Service is already in a terminal state and cannot be canceled");
        }

        service.Cancel(request.Reason, request.CanceledAt);
        unitOfWork.Services.Update(service);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
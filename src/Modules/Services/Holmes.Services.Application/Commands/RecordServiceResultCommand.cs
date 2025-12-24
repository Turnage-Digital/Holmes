using Holmes.Core.Application;
using Holmes.Core.Domain;
using Holmes.Core.Domain.ValueObjects;
using Holmes.Services.Domain;
using MediatR;

namespace Holmes.Services.Application.Commands;

public sealed record RecordServiceResultCommand(
    UlidId ServiceId,
    ServiceResult Result,
    DateTimeOffset CompletedAt
) : RequestBase<Result>;

public sealed class RecordServiceResultCommandHandler(
    IServicesUnitOfWork unitOfWork
) : IRequestHandler<RecordServiceResultCommand, Result>
{
    public async Task<Result> Handle(
        RecordServiceResultCommand request,
        CancellationToken cancellationToken
    )
    {
        var service = await unitOfWork.Services.GetByIdAsync(
            request.ServiceId, cancellationToken);

        if (service is null)
        {
            return Result.Fail($"Service {request.ServiceId} not found");
        }

        if (service.IsTerminal)
        {
            return Result.Fail("Service is already in a terminal state");
        }

        service.RecordResult(request.Result, request.CompletedAt);
        unitOfWork.Services.Update(service);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
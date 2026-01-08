using Holmes.Core.Application;
using Holmes.Services.Domain;
using MediatR;

namespace Holmes.Services.Application.Commands;

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
            return Result.Fail(ResultErrors.NotFound);
        }

        if (service.IsTerminal)
        {
            return Result.Fail(ResultErrors.Validation);
        }

        service.RecordResult(request.Result, request.CompletedAt);
        unitOfWork.Services.Update(service);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
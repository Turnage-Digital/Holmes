using Holmes.Core.Domain;
using Holmes.Services.Application.Abstractions;
using Holmes.Services.Application.Abstractions.Commands;
using Holmes.Services.Domain;
using MediatR;

namespace Holmes.Services.Application.Commands;

public sealed class ProcessVendorCallbackCommandHandler(
    IServicesUnitOfWork unitOfWork,
    IVendorAdapterFactory vendorAdapterFactory
) : IRequestHandler<ProcessVendorCallbackCommand, Result>
{
    public async Task<Result> Handle(
        ProcessVendorCallbackCommand request,
        CancellationToken cancellationToken
    )
    {
        var vendorAdapter = vendorAdapterFactory.GetAdapter(request.VendorCode);
        if (vendorAdapter is null)
        {
            return Result.Fail($"Vendor adapter '{request.VendorCode}' not found");
        }

        // Find the service by vendor reference
        var service = await unitOfWork.Services.GetByVendorReferenceAsync(
            request.VendorCode, request.VendorReferenceId, cancellationToken);

        if (service is null)
        {
            return Result.Fail($"Service not found for vendor reference {request.VendorReferenceId}");
        }

        if (service.IsTerminal)
        {
            // Already completed, idempotent
            return Result.Success();
        }

        // Parse the callback payload
        var result = await vendorAdapter.ParseCallbackAsync(request.CallbackPayload, cancellationToken);

        // Record the result
        service.RecordResult(result, request.ReceivedAt);
        unitOfWork.Services.Update(service);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
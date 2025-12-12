using Holmes.Core.Application;
using Holmes.Core.Domain;
using Holmes.Services.Application.Abstractions;
using Holmes.Services.Domain;
using MediatR;

namespace Holmes.Services.Application.Commands;

public sealed record ProcessVendorCallbackCommand(
    string VendorCode,
    string VendorReferenceId,
    string CallbackPayload,
    DateTimeOffset ReceivedAt
) : RequestBase<Result>;

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

        // Find the service request by vendor reference
        var serviceRequest = await unitOfWork.ServiceRequests.GetByVendorReferenceAsync(
            request.VendorCode, request.VendorReferenceId, cancellationToken);

        if (serviceRequest is null)
        {
            return Result.Fail($"Service request not found for vendor reference {request.VendorReferenceId}");
        }

        if (serviceRequest.IsTerminal)
        {
            // Already completed, idempotent
            return Result.Success();
        }

        // Parse the callback payload
        var result = await vendorAdapter.ParseCallbackAsync(request.CallbackPayload, cancellationToken);

        // Record the result
        serviceRequest.RecordResult(result, request.ReceivedAt);
        unitOfWork.ServiceRequests.Update(serviceRequest);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
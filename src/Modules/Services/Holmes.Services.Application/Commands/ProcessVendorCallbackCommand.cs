using Holmes.Core.Application.Abstractions;
using Holmes.Core.Domain;

namespace Holmes.Services.Application.Commands;

public sealed record ProcessVendorCallbackCommand(
    string VendorCode,
    string VendorReferenceId,
    string CallbackPayload,
    DateTimeOffset ReceivedAt
) : RequestBase<Result>;
using Holmes.Core.Contracts;
using Holmes.Core.Application;
using Holmes.Core.Domain.ValueObjects;
using Holmes.Services.Contracts.Dtos;

namespace Holmes.Services.Application.Queries;

public sealed record GetServiceQuery(
    UlidId ServiceId
) : RequestBase<Result<ServiceSummaryDto>>;
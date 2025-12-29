using Holmes.Core.Application.Abstractions;
using Holmes.Core.Domain;
using Holmes.Core.Domain.ValueObjects;
using Holmes.Services.Application.Abstractions.Dtos;

namespace Holmes.Services.Application.Queries;

public sealed record GetServiceQuery(
    UlidId ServiceId
) : RequestBase<Result<ServiceSummaryDto>>;
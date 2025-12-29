using Holmes.Core.Application.Abstractions;
using Holmes.Core.Domain;
using Holmes.Core.Domain.ValueObjects;
using Holmes.Services.Application.Abstractions.Dtos;

namespace Holmes.Services.Application.Queries;

public sealed record GetServicesByOrderQuery(
    UlidId OrderId
) : RequestBase<Result<IReadOnlyList<ServiceSummaryDto>>>;
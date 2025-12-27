using Holmes.Core.Application;
using Holmes.Core.Domain;
using Holmes.Core.Domain.ValueObjects;
using Holmes.Services.Application.Abstractions.Dtos;

namespace Holmes.Services.Application.Abstractions.Queries;

public sealed record GetServicesByOrderQuery(
    UlidId OrderId
) : RequestBase<Result<IReadOnlyList<ServiceSummaryDto>>>;

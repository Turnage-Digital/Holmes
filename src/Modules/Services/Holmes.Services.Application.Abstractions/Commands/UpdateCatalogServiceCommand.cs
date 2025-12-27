using Holmes.Core.Domain;
using Holmes.Core.Domain.ValueObjects;
using MediatR;

namespace Holmes.Services.Application.Abstractions.Commands;

/// <summary>
///     Updates a specific service configuration in a customer's catalog.
/// </summary>
public sealed record UpdateCatalogServiceCommand(
    string CustomerId,
    string ServiceTypeCode,
    bool IsEnabled,
    int? Tier,
    string? VendorCode,
    UlidId UpdatedBy
) : IRequest<Result>;

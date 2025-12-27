using Holmes.Core.Domain;
using Holmes.Core.Domain.ValueObjects;
using MediatR;

namespace Holmes.Services.Application.Abstractions.Commands;

/// <summary>
///     Updates a tier configuration in a customer's catalog.
/// </summary>
public sealed record UpdateTierConfigurationCommand(
    string CustomerId,
    int Tier,
    string? Name,
    string? Description,
    IReadOnlyCollection<string>? RequiredServices,
    IReadOnlyCollection<string>? OptionalServices,
    bool? AutoDispatch,
    bool? WaitForPreviousTier,
    UlidId UpdatedBy
) : IRequest<Result>;
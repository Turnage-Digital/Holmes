using Holmes.Core.Application;
using Holmes.Core.Domain;
using Holmes.Services.Application.Abstractions.Dtos;
using Holmes.Services.Domain;
using MediatR;

namespace Holmes.Services.Application.Queries;

public sealed record ListServiceTypesQuery : RequestBase<Result<IReadOnlyCollection<ServiceTypeDto>>>;

public sealed class ListServiceTypesQueryHandler
    : IRequestHandler<ListServiceTypesQuery, Result<IReadOnlyCollection<ServiceTypeDto>>>
{
    private static readonly IReadOnlyCollection<ServiceTypeDto> ServiceTypes = new List<ServiceTypeDto>
    {
        new("SSN_TRACE", "SSN Trace", ServiceCategory.Identity, 1),
        new("NATL_CRIM", "National Criminal Search", ServiceCategory.Criminal, 1),
        new("STATE_CRIM", "State Criminal Search", ServiceCategory.Criminal, 2),
        new("COUNTY_CRIM", "County Criminal Search", ServiceCategory.Criminal, 2),
        new("FED_CRIM", "Federal Criminal Search", ServiceCategory.Criminal, 2),
        new("SEX_OFFENDER", "Sex Offender Registry", ServiceCategory.Criminal, 1),
        new("GLOBAL_WATCH", "Global Watchlist Search", ServiceCategory.Criminal, 1),
        new("MVR", "Motor Vehicle Report", ServiceCategory.Driving, 2),
        new("CREDIT_CHECK", "Credit Report", ServiceCategory.Credit, 3),
        new("TWN_EMP", "Employment Verification (TWN)", ServiceCategory.Employment, 3),
        new("MANUAL_EMP", "Employment Verification (Manual)", ServiceCategory.Employment, 3),
        new("EDU_VERIFY", "Education Verification", ServiceCategory.Education, 3),
        new("PRO_LICENSE", "Professional License Verification", ServiceCategory.Employment, 3),
        new("REF_CHECK", "Reference Check", ServiceCategory.Reference, 4),
        new("DRUG_5", "5-Panel Drug Screen", ServiceCategory.Drug, 4),
        new("DRUG_10", "10-Panel Drug Screen", ServiceCategory.Drug, 4),
        new("CIVIL_COURT", "Civil Court Search", ServiceCategory.Civil, 2),
        new("BANKRUPTCY", "Bankruptcy Search", ServiceCategory.Civil, 2),
        new("HEALTHCARE_SANCTION", "Healthcare Sanctions Check", ServiceCategory.Healthcare, 2)
    };

    public Task<Result<IReadOnlyCollection<ServiceTypeDto>>> Handle(
        ListServiceTypesQuery request,
        CancellationToken cancellationToken
    )
    {
        return Task.FromResult(Result.Success(ServiceTypes));
    }
}
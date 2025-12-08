using Holmes.App.Infrastructure.Security;
using Holmes.App.Server.Contracts;
using Holmes.Core.Application;
using Holmes.Core.Domain.ValueObjects;
using Holmes.Subjects.Application.Abstractions.Dtos;
using Holmes.Subjects.Application.Abstractions.Queries;
using Holmes.Subjects.Application.Commands;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Holmes.App.Server.Controllers;

[ApiController]
[Route("api/subjects")]
[Authorize(Policy = AuthorizationPolicies.RequireOps)]
public sealed class SubjectsController(
    IMediator mediator,
    ISubjectQueries subjectQueries,
    ICurrentUserInitializer currentUserInitializer
) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PaginatedResponse<SubjectListItemDto>>> GetSubjects(
        [FromQuery] PaginationQuery query,
        CancellationToken cancellationToken
    )
    {
        var (page, pageSize) = PaginationNormalization.Normalize(query);

        var result = await subjectQueries.GetSubjectsPagedAsync(page, pageSize, cancellationToken);

        return Ok(PaginatedResponse<SubjectListItemDto>.Create(
            result.Items.ToList(), page, pageSize, result.TotalCount));
    }

    [HttpPost]
    public async Task<ActionResult<SubjectSummaryDto>> RegisterSubject(
        [FromBody] RegisterSubjectRequest request,
        CancellationToken cancellationToken
    )
    {
        var subjectId = await mediator.Send(new RegisterSubjectCommand(
            request.GivenName,
            request.FamilyName,
            request.DateOfBirth,
            request.Email,
            DateTimeOffset.UtcNow), cancellationToken);

        var summary = await subjectQueries.GetSummaryByIdAsync(subjectId.ToString(), cancellationToken);

        if (summary is null)
        {
            return Problem("Failed to load created subject.");
        }

        return CreatedAtAction(nameof(GetSubjectById), new { subjectId = subjectId.ToString() }, summary);
    }

    [HttpGet("{subjectId}")]
    public async Task<ActionResult<SubjectDetailDto>> GetSubjectById(
        string subjectId,
        CancellationToken cancellationToken
    )
    {
        if (!Ulid.TryParse(subjectId, out _))
        {
            return BadRequest("Invalid subject id format.");
        }

        var detail = await subjectQueries.GetDetailByIdAsync(subjectId, cancellationToken);

        if (detail is null)
        {
            return NotFound();
        }

        return Ok(detail);
    }

    [HttpGet("{subjectId}/addresses")]
    public async Task<ActionResult<IReadOnlyCollection<SubjectAddressDto>>> GetAddresses(
        string subjectId,
        CancellationToken cancellationToken
    )
    {
        if (!Ulid.TryParse(subjectId, out _))
        {
            return BadRequest("Invalid subject id format.");
        }

        var addresses = await subjectQueries.GetAddressesAsync(subjectId, cancellationToken);
        return Ok(addresses);
    }

    [HttpGet("{subjectId}/employments")]
    public async Task<ActionResult<IReadOnlyCollection<SubjectEmploymentDto>>> GetEmployments(
        string subjectId,
        CancellationToken cancellationToken
    )
    {
        if (!Ulid.TryParse(subjectId, out _))
        {
            return BadRequest("Invalid subject id format.");
        }

        var employments = await subjectQueries.GetEmploymentsAsync(subjectId, cancellationToken);
        return Ok(employments);
    }

    [HttpGet("{subjectId}/educations")]
    public async Task<ActionResult<IReadOnlyCollection<SubjectEducationDto>>> GetEducations(
        string subjectId,
        CancellationToken cancellationToken
    )
    {
        if (!Ulid.TryParse(subjectId, out _))
        {
            return BadRequest("Invalid subject id format.");
        }

        var educations = await subjectQueries.GetEducationsAsync(subjectId, cancellationToken);
        return Ok(educations);
    }

    [HttpGet("{subjectId}/references")]
    public async Task<ActionResult<IReadOnlyCollection<SubjectReferenceDto>>> GetReferences(
        string subjectId,
        CancellationToken cancellationToken
    )
    {
        if (!Ulid.TryParse(subjectId, out _))
        {
            return BadRequest("Invalid subject id format.");
        }

        var references = await subjectQueries.GetReferencesAsync(subjectId, cancellationToken);
        return Ok(references);
    }

    [HttpGet("{subjectId}/phones")]
    public async Task<ActionResult<IReadOnlyCollection<SubjectPhoneDto>>> GetPhones(
        string subjectId,
        CancellationToken cancellationToken
    )
    {
        if (!Ulid.TryParse(subjectId, out _))
        {
            return BadRequest("Invalid subject id format.");
        }

        var phones = await subjectQueries.GetPhonesAsync(subjectId, cancellationToken);
        return Ok(phones);
    }

    [HttpPost("merge")]
    public async Task<IActionResult> MergeSubjects(
        [FromBody] MergeSubjectsRequest request,
        CancellationToken cancellationToken
    )
    {
        if (!Ulid.TryParse(request.WinnerSubjectId, out var winner) ||
            !Ulid.TryParse(request.MergedSubjectId, out var merged))
        {
            return BadRequest("Invalid subject id format.");
        }

        if (winner == merged)
        {
            return BadRequest("Winner and merged subject ids must differ.");
        }

        var actor = await GetCurrentUserAsync(cancellationToken);
        var result = await mediator.Send(new MergeSubjectCommand(
            UlidId.FromUlid(merged),
            UlidId.FromUlid(winner),
            DateTimeOffset.UtcNow), cancellationToken);

        return result.IsSuccess ? NoContent() : BadRequest(result.Error);
    }

    private async Task<UlidId> GetCurrentUserAsync(CancellationToken cancellationToken)
    {
        return await currentUserInitializer.EnsureCurrentUserIdAsync(cancellationToken);
    }

    public sealed record RegisterSubjectRequest(
        string GivenName,
        string FamilyName,
        DateOnly? DateOfBirth,
        string? Email
    );

    public sealed record MergeSubjectsRequest(
        string WinnerSubjectId,
        string MergedSubjectId,
        string? Reason
    );
}
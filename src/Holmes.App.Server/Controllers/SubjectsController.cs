using Holmes.App.Infrastructure.Security;
using Holmes.App.Server.Contracts;
using Holmes.Core.Domain.ValueObjects;
using Holmes.Subjects.Application.Abstractions.Dtos;
using Holmes.Subjects.Application.Commands;
using Holmes.Subjects.Application.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Holmes.App.Server.Controllers;

[ApiController]
[Route("api/subjects")]
[Authorize(Policy = AuthorizationPolicies.RequireOps)]
public sealed class SubjectsController(
    IMediator mediator
) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PaginatedResponse<SubjectListItemDto>>> GetSubjects(
        [FromQuery] PaginationQuery query,
        CancellationToken cancellationToken
    )
    {
        var (page, pageSize) = PaginationNormalization.Normalize(query);

        var result = await mediator.Send(new ListSubjectsQuery(page, pageSize), cancellationToken);

        if (!result.IsSuccess)
        {
            return Problem(result.Error);
        }

        return Ok(PaginatedResponse<SubjectListItemDto>.Create(
            result.Value.Items.ToList(), page, pageSize, result.Value.TotalCount));
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

        var summaryResult = await mediator.Send(
            new GetSubjectSummaryQuery(subjectId.ToString()), cancellationToken);

        if (!summaryResult.IsSuccess)
        {
            return Problem("Failed to load created subject.");
        }

        return CreatedAtAction(nameof(GetSubjectById), new { subjectId = subjectId.ToString() }, summaryResult.Value);
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

        var result = await mediator.Send(new GetSubjectByIdQuery(subjectId), cancellationToken);

        if (!result.IsSuccess)
        {
            return NotFound();
        }

        return Ok(result.Value);
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

        var result = await mediator.Send(new GetSubjectAddressesQuery(subjectId), cancellationToken);
        if (!result.IsSuccess)
        {
            return Problem(result.Error);
        }

        return Ok(result.Value);
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

        var result = await mediator.Send(new GetSubjectEmploymentsQuery(subjectId), cancellationToken);
        if (!result.IsSuccess)
        {
            return Problem(result.Error);
        }

        return Ok(result.Value);
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

        var result = await mediator.Send(new GetSubjectEducationsQuery(subjectId), cancellationToken);
        if (!result.IsSuccess)
        {
            return Problem(result.Error);
        }

        return Ok(result.Value);
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

        var result = await mediator.Send(new GetSubjectReferencesQuery(subjectId), cancellationToken);
        if (!result.IsSuccess)
        {
            return Problem(result.Error);
        }

        return Ok(result.Value);
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

        var result = await mediator.Send(new GetSubjectPhonesQuery(subjectId), cancellationToken);
        if (!result.IsSuccess)
        {
            return Problem(result.Error);
        }

        return Ok(result.Value);
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

        var result = await mediator.Send(new MergeSubjectCommand(
            UlidId.FromUlid(merged),
            UlidId.FromUlid(winner),
            DateTimeOffset.UtcNow), cancellationToken);

        return result.IsSuccess ? NoContent() : BadRequest(result.Error);
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
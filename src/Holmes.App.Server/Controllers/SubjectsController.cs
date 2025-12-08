using Holmes.App.Infrastructure.Security;
using Holmes.App.Server.Contracts;
using Holmes.Core.Application;
using Holmes.Core.Domain.ValueObjects;
using Holmes.Subjects.Application.Abstractions.Dtos;
using Holmes.Subjects.Application.Commands;
using Holmes.Subjects.Infrastructure.Sql;
using Holmes.Subjects.Infrastructure.Sql.Mappers;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Holmes.App.Server.Controllers;

[ApiController]
[Route("api/subjects")]
[Authorize(Policy = AuthorizationPolicies.RequireOps)]
public sealed class SubjectsController(
    IMediator mediator,
    SubjectsDbContext dbContext,
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

        var baseQuery = dbContext.Subjects
            .AsNoTracking()
            .Include(x => x.Aliases);

        var totalItems = await baseQuery.CountAsync(cancellationToken);
        var subjects = await baseQuery
            .OrderBy(x => x.FamilyName)
            .ThenBy(x => x.GivenName)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var items = subjects
            .Select(SubjectMapper.ToListItem)
            .ToList();

        return Ok(PaginatedResponse<SubjectListItemDto>.Create(items, page, pageSize, totalItems));
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

        var directory = await dbContext.SubjectDirectory.AsNoTracking()
            .SingleOrDefaultAsync(x => x.SubjectId == subjectId.ToString(), cancellationToken);

        if (directory is null)
        {
            return Problem("Failed to load created subject.");
        }

        return CreatedAtAction(nameof(GetSubjectById), new { subjectId = subjectId.ToString() },
            SubjectMapper.ToSummary(directory));
    }

    [HttpGet("{subjectId}")]
    public async Task<ActionResult<SubjectDetailDto>> GetSubjectById(
        string subjectId,
        CancellationToken cancellationToken
    )
    {
        if (!Ulid.TryParse(subjectId, out var parsed))
        {
            return BadRequest("Invalid subject id format.");
        }

        var subject = await dbContext.Subjects
            .AsNoTracking()
            .Include(x => x.Aliases)
            .Include(x => x.Addresses)
            .Include(x => x.Employments)
            .Include(x => x.Educations)
            .Include(x => x.References)
            .Include(x => x.Phones)
            .SingleOrDefaultAsync(x => x.SubjectId == parsed.ToString(), cancellationToken);

        if (subject is null)
        {
            return NotFound();
        }

        return Ok(SubjectMapper.ToDetail(subject));
    }

    [HttpGet("{subjectId}/addresses")]
    public async Task<ActionResult<IReadOnlyCollection<SubjectAddressDto>>> GetAddresses(
        string subjectId,
        CancellationToken cancellationToken
    )
    {
        if (!Ulid.TryParse(subjectId, out var parsed))
        {
            return BadRequest("Invalid subject id format.");
        }

        var addresses = await dbContext.SubjectAddresses
            .AsNoTracking()
            .Where(x => x.SubjectId == parsed.ToString())
            .OrderByDescending(x => x.ToDate == null)
            .ThenByDescending(x => x.FromDate)
            .ToListAsync(cancellationToken);

        return Ok(addresses.Select(SubjectMapper.ToAddressDto).ToList());
    }

    [HttpGet("{subjectId}/employments")]
    public async Task<ActionResult<IReadOnlyCollection<SubjectEmploymentDto>>> GetEmployments(
        string subjectId,
        CancellationToken cancellationToken
    )
    {
        if (!Ulid.TryParse(subjectId, out var parsed))
        {
            return BadRequest("Invalid subject id format.");
        }

        var employments = await dbContext.SubjectEmployments
            .AsNoTracking()
            .Where(x => x.SubjectId == parsed.ToString())
            .OrderByDescending(x => x.EndDate == null)
            .ThenByDescending(x => x.StartDate)
            .ToListAsync(cancellationToken);

        return Ok(employments.Select(SubjectMapper.ToEmploymentDto).ToList());
    }

    [HttpGet("{subjectId}/educations")]
    public async Task<ActionResult<IReadOnlyCollection<SubjectEducationDto>>> GetEducations(
        string subjectId,
        CancellationToken cancellationToken
    )
    {
        if (!Ulid.TryParse(subjectId, out var parsed))
        {
            return BadRequest("Invalid subject id format.");
        }

        var educations = await dbContext.SubjectEducations
            .AsNoTracking()
            .Where(x => x.SubjectId == parsed.ToString())
            .OrderByDescending(x => x.GraduationDate ?? x.AttendedTo)
            .ToListAsync(cancellationToken);

        return Ok(educations.Select(SubjectMapper.ToEducationDto).ToList());
    }

    [HttpGet("{subjectId}/references")]
    public async Task<ActionResult<IReadOnlyCollection<SubjectReferenceDto>>> GetReferences(
        string subjectId,
        CancellationToken cancellationToken
    )
    {
        if (!Ulid.TryParse(subjectId, out var parsed))
        {
            return BadRequest("Invalid subject id format.");
        }

        var references = await dbContext.SubjectReferences
            .AsNoTracking()
            .Where(x => x.SubjectId == parsed.ToString())
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync(cancellationToken);

        return Ok(references.Select(SubjectMapper.ToReferenceDto).ToList());
    }

    [HttpGet("{subjectId}/phones")]
    public async Task<ActionResult<IReadOnlyCollection<SubjectPhoneDto>>> GetPhones(
        string subjectId,
        CancellationToken cancellationToken
    )
    {
        if (!Ulid.TryParse(subjectId, out var parsed))
        {
            return BadRequest("Invalid subject id format.");
        }

        var phones = await dbContext.SubjectPhones
            .AsNoTracking()
            .Where(x => x.SubjectId == parsed.ToString())
            .OrderByDescending(x => x.IsPrimary)
            .ThenByDescending(x => x.CreatedAt)
            .ToListAsync(cancellationToken);

        return Ok(phones.Select(SubjectMapper.ToPhoneDto).ToList());
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
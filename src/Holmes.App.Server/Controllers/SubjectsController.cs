using System.Globalization;
using Holmes.App.Server.Contracts;
using Holmes.App.Server.Security;
using Holmes.Core.Application;
using Holmes.Core.Domain.ValueObjects;
using Holmes.Subjects.Application.Abstractions.Dtos;
using Holmes.Subjects.Application.Commands;
using Holmes.Subjects.Infrastructure.Sql;
using Holmes.Subjects.Infrastructure.Sql.Entities;
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
    private const string SubjectStatusActive = "Active";
    private const string SubjectStatusMerged = "Merged";

    [HttpGet]
    public async Task<ActionResult<PaginatedResponse<SubjectListItemDto>>> GetSubjects(
        [FromQuery] PaginationQuery query,
        CancellationToken cancellationToken
    )
    {
        var (page, pageSize) = NormalizePagination(query);

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
            .Select(MapSubject)
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
            .SingleAsync(x => x.SubjectId == subjectId.ToString(), cancellationToken);

        return CreatedAtAction(nameof(GetSubjectById), new { subjectId = subjectId.ToString() }, MapSummary(directory));
    }

    [HttpGet("{subjectId}")]
    public async Task<ActionResult<SubjectSummaryDto>> GetSubjectById(
        string subjectId,
        CancellationToken cancellationToken
    )
    {
        if (!Ulid.TryParse(subjectId, out var parsed))
        {
            return BadRequest("Invalid subject id format.");
        }

        var directory = await dbContext.SubjectDirectory.AsNoTracking()
            .SingleOrDefaultAsync(x => x.SubjectId == parsed.ToString(), cancellationToken);

        if (directory is null)
        {
            return NotFound();
        }

        return Ok(MapSummary(directory));
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

    private static SubjectSummaryDto MapSummary(SubjectDirectoryDb directory)
    {
        return new SubjectSummaryDto(directory.SubjectId, directory.GivenName, directory.FamilyName,
            directory.DateOfBirth,
            directory.Email, directory.IsMerged, directory.AliasCount, directory.CreatedAt);
    }

    private static SubjectListItemDto MapSubject(SubjectDb subject)
    {
        var status = subject.MergedIntoSubjectId is null ? SubjectStatusActive : SubjectStatusMerged;
        var aliases = subject.Aliases
            .OrderBy(a => a.FamilyName)
            .ThenBy(a => a.GivenName)
            .Select(a => new SubjectAliasDto(
                a.Id.ToString(CultureInfo.InvariantCulture),
                a.GivenName,
                a.FamilyName,
                a.DateOfBirth,
                subject.CreatedAt))
            .ToList();

        return new SubjectListItemDto(
            subject.SubjectId,
            subject.GivenName,
            null,
            subject.FamilyName,
            subject.DateOfBirth,
            subject.Email,
            status,
            subject.MergedIntoSubjectId,
            aliases,
            subject.CreatedAt,
            subject.MergedAt ?? subject.CreatedAt);
    }

    private static (int Page, int PageSize) NormalizePagination(PaginationQuery query)
    {
        var page = query.Page <= 0 ? 1 : query.Page;
        var size = query.PageSize <= 0 ? 25 : Math.Min(query.PageSize, 100);
        return (page, size);
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
using Holmes.Subjects.Application.Commands;
using Holmes.Subjects.Infrastructure.Sql;
using Holmes.Subjects.Infrastructure.Sql.Entities;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Holmes.App.Server.Controllers;

[ApiController]
[Route("subjects")]
[Authorize]
public sealed class SubjectsController : ControllerBase
{
    private readonly SubjectsDbContext _dbContext;
    private readonly IMediator _mediator;

    public SubjectsController(IMediator mediator, SubjectsDbContext dbContext)
    {
        _mediator = mediator;
        _dbContext = dbContext;
    }

    [HttpPost]
    public async Task<ActionResult<SubjectSummaryResponse>> RegisterSubject(
        [FromBody] RegisterSubjectRequest request,
        CancellationToken cancellationToken
    )
    {
        var subjectId = await _mediator.Send(new RegisterSubjectCommand(
            request.GivenName,
            request.FamilyName,
            request.DateOfBirth,
            request.Email,
            DateTimeOffset.UtcNow), cancellationToken);

        var directory = await _dbContext.SubjectDirectory.AsNoTracking()
            .SingleAsync(x => x.SubjectId == subjectId.ToString(), cancellationToken);

        return CreatedAtAction(nameof(GetSubjectById), new { subjectId = subjectId.ToString() }, MapSummary(directory));
    }

    [HttpGet("{subjectId}")]
    public async Task<ActionResult<SubjectSummaryResponse>> GetSubjectById(
        string subjectId,
        CancellationToken cancellationToken
    )
    {
        if (!Ulid.TryParse(subjectId, out var parsed))
        {
            return BadRequest("Invalid subject id format.");
        }

        var directory = await _dbContext.SubjectDirectory.AsNoTracking()
            .SingleOrDefaultAsync(x => x.SubjectId == parsed.ToString(), cancellationToken);

        if (directory is null)
        {
            return NotFound();
        }

        return Ok(MapSummary(directory));
    }

    private static SubjectSummaryResponse MapSummary(SubjectDirectoryDb directory)
    {
        return new SubjectSummaryResponse(directory.SubjectId, directory.GivenName, directory.FamilyName,
            directory.DateOfBirth,
            directory.Email, directory.IsMerged, directory.AliasCount, directory.CreatedAt);
    }

    public sealed record RegisterSubjectRequest(
        string GivenName,
        string FamilyName,
        DateOnly? DateOfBirth,
        string? Email
    );

    public sealed record SubjectSummaryResponse(
        string SubjectId,
        string GivenName,
        string FamilyName,
        DateOnly? DateOfBirth,
        string? Email,
        bool IsMerged,
        int AliasCount,
        DateTimeOffset CreatedAt
    );
}
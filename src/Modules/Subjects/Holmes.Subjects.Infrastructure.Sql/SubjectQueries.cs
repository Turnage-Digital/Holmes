using Holmes.Subjects.Contracts;
using Holmes.Subjects.Contracts.Dtos;
using Holmes.Subjects.Infrastructure.Sql.Mappers;
using Microsoft.EntityFrameworkCore;

namespace Holmes.Subjects.Infrastructure.Sql;

/// <summary>
///     Read-side queries for Subjects using projection tables (CQRS pattern).
/// </summary>
public sealed class SubjectQueries(SubjectsDbContext dbContext) : ISubjectQueries
{
    public async Task<SubjectPagedResult> GetSubjectsPagedAsync(
        int page,
        int pageSize,
        CancellationToken cancellationToken
    )
    {
        // Read from projection table (read model) for list view
        var baseQuery = dbContext.SubjectProjections.AsNoTracking();

        var totalItems = await baseQuery.CountAsync(cancellationToken);
        var projections = await baseQuery
            .OrderBy(x => x.FamilyName)
            .ThenBy(x => x.GivenName)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var items = projections
            .Select(SubjectMapper.ToListItemFromProjection)
            .ToList();

        return new SubjectPagedResult(items, totalItems);
    }

    public async Task<SubjectSummaryDto?> GetSummaryByIdAsync(string subjectId, CancellationToken cancellationToken)
    {
        var directory = await dbContext.SubjectProjections.AsNoTracking()
            .SingleOrDefaultAsync(x => x.SubjectId == subjectId, cancellationToken);

        return directory is null ? null : SubjectMapper.ToSummary(directory);
    }

    public async Task<SubjectDetailDto?> GetDetailByIdAsync(string subjectId, CancellationToken cancellationToken)
    {
        var subject = await dbContext.Subjects
            .AsNoTracking()
            .Include(x => x.Aliases)
            .Include(x => x.Addresses)
            .Include(x => x.Employments)
            .Include(x => x.Educations)
            .Include(x => x.References)
            .Include(x => x.Phones)
            .SingleOrDefaultAsync(x => x.SubjectId == subjectId, cancellationToken);

        return subject is null ? null : SubjectMapper.ToDetail(subject);
    }

    public async Task<IReadOnlyList<SubjectAddressDto>> GetAddressesAsync(
        string subjectId,
        CancellationToken cancellationToken
    )
    {
        var addresses = await dbContext.SubjectAddresses
            .AsNoTracking()
            .Where(x => x.SubjectId == subjectId)
            .OrderByDescending(x => x.ToDate == null)
            .ThenByDescending(x => x.FromDate)
            .ToListAsync(cancellationToken);

        return addresses.Select(SubjectMapper.ToAddressDto).ToList();
    }

    public async Task<IReadOnlyList<SubjectEmploymentDto>> GetEmploymentsAsync(
        string subjectId,
        CancellationToken cancellationToken
    )
    {
        var employments = await dbContext.SubjectEmployments
            .AsNoTracking()
            .Where(x => x.SubjectId == subjectId)
            .OrderByDescending(x => x.EndDate == null)
            .ThenByDescending(x => x.StartDate)
            .ToListAsync(cancellationToken);

        return employments.Select(SubjectMapper.ToEmploymentDto).ToList();
    }

    public async Task<IReadOnlyList<SubjectEducationDto>> GetEducationsAsync(
        string subjectId,
        CancellationToken cancellationToken
    )
    {
        var educations = await dbContext.SubjectEducations
            .AsNoTracking()
            .Where(x => x.SubjectId == subjectId)
            .OrderByDescending(x => x.GraduationDate ?? x.AttendedTo)
            .ToListAsync(cancellationToken);

        return educations.Select(SubjectMapper.ToEducationDto).ToList();
    }

    public async Task<IReadOnlyList<SubjectReferenceDto>> GetReferencesAsync(
        string subjectId,
        CancellationToken cancellationToken
    )
    {
        var references = await dbContext.SubjectReferences
            .AsNoTracking()
            .Where(x => x.SubjectId == subjectId)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync(cancellationToken);

        return references.Select(SubjectMapper.ToReferenceDto).ToList();
    }

    public async Task<IReadOnlyList<SubjectPhoneDto>> GetPhonesAsync(
        string subjectId,
        CancellationToken cancellationToken
    )
    {
        var phones = await dbContext.SubjectPhones
            .AsNoTracking()
            .Where(x => x.SubjectId == subjectId)
            .OrderByDescending(x => x.IsPrimary)
            .ThenByDescending(x => x.CreatedAt)
            .ToListAsync(cancellationToken);

        return phones.Select(SubjectMapper.ToPhoneDto).ToList();
    }

    public async Task<bool> ExistsAsync(string subjectId, CancellationToken cancellationToken)
    {
        return await dbContext.Subjects
            .AsNoTracking()
            .AnyAsync(x => x.SubjectId == subjectId, cancellationToken);
    }
}
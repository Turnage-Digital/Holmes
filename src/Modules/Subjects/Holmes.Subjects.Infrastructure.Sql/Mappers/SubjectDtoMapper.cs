using System.Globalization;
using Holmes.Subjects.Application.Abstractions.Dtos;
using Holmes.Subjects.Infrastructure.Sql.Entities;

namespace Holmes.Subjects.Infrastructure.Sql.Mappers;

public static class SubjectDtoMapper
{
    public static SubjectSummaryDto ToSummary(SubjectDirectoryDb directory)
    {
        return new SubjectSummaryDto(
            directory.SubjectId,
            directory.GivenName,
            directory.FamilyName,
            directory.DateOfBirth,
            directory.Email,
            directory.IsMerged,
            directory.AliasCount,
            directory.CreatedAt);
    }

    public static SubjectListItemDto ToListItem(SubjectDb subject)
    {
        var status = subject.MergedIntoSubjectId is null ? "Active" : "Merged";
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
}
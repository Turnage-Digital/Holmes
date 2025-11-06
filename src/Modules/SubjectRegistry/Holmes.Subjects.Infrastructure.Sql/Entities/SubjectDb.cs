using Holmes.Core.Domain.ValueObjects;

namespace Holmes.Subjects.Infrastructure.Sql.Entities;

public class SubjectDb
{
    public string SubjectId { get; set; } = null!;

    public string GivenName { get; set; } = null!;

    public string FamilyName { get; set; } = null!;

    public DateOnly? DateOfBirth { get; set; }

    public string? Email { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public string? MergedIntoSubjectId { get; set; }

    public UlidId? MergedBy { get; set; }

    public DateTimeOffset? MergedAt { get; set; }

    public ICollection<SubjectAliasDb> Aliases { get; set; } = new List<SubjectAliasDb>();
}
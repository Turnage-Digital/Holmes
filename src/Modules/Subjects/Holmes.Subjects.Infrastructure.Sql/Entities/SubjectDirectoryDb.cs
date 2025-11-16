namespace Holmes.Subjects.Infrastructure.Sql.Entities;

public class SubjectDirectoryDb
{
    public string SubjectId { get; set; } = null!;

    public string GivenName { get; set; } = null!;

    public string FamilyName { get; set; } = null!;

    public DateOnly? DateOfBirth { get; set; }

    public string? Email { get; set; }

    public bool IsMerged { get; set; }

    public int AliasCount { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
}

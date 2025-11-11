namespace Holmes.Subjects.Infrastructure.Sql.Entities;

public class SubjectAliasDb
{
    public long Id { get; set; }

    public string SubjectId { get; set; } = null!;

    public string GivenName { get; set; } = null!;

    public string FamilyName { get; set; } = null!;

    public DateOnly? DateOfBirth { get; set; }

    public SubjectDb Subject { get; set; } = null!;
}
namespace Holmes.Subjects.Infrastructure.Sql.Entities;

public class SubjectReferenceDb
{
    public string Id { get; set; } = null!;

    public string SubjectId { get; set; } = null!;

    public string Name { get; set; } = null!;

    public string? Phone { get; set; }

    public string? Email { get; set; }

    public string? Relationship { get; set; }

    public int? YearsKnown { get; set; }

    public int ReferenceType { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public SubjectDb Subject { get; set; } = null!;
}
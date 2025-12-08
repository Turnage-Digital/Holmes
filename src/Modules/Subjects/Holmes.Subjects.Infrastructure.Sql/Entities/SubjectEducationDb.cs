namespace Holmes.Subjects.Infrastructure.Sql.Entities;

public class SubjectEducationDb
{
    public string Id { get; set; } = null!;

    public string SubjectId { get; set; } = null!;

    public string InstitutionName { get; set; } = null!;

    public string? InstitutionAddress { get; set; }

    public string? Degree { get; set; }

    public string? Major { get; set; }

    public DateOnly? AttendedFrom { get; set; }

    public DateOnly? AttendedTo { get; set; }

    public DateOnly? GraduationDate { get; set; }

    public bool Graduated { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public SubjectDb Subject { get; set; } = null!;
}
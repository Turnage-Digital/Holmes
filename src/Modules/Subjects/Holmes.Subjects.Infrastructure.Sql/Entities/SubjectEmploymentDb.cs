namespace Holmes.Subjects.Infrastructure.Sql.Entities;

public class SubjectEmploymentDb
{
    public string Id { get; set; } = null!;

    public string SubjectId { get; set; } = null!;

    public string EmployerName { get; set; } = null!;

    public string? EmployerPhone { get; set; }

    public string? EmployerAddress { get; set; }

    public string? JobTitle { get; set; }

    public string? SupervisorName { get; set; }

    public string? SupervisorPhone { get; set; }

    public DateOnly StartDate { get; set; }

    public DateOnly? EndDate { get; set; }

    public string? ReasonForLeaving { get; set; }

    public bool CanContact { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public SubjectDb Subject { get; set; } = null!;
}

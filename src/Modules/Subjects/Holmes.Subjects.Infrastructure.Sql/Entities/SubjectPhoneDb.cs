namespace Holmes.Subjects.Infrastructure.Sql.Entities;

public class SubjectPhoneDb
{
    public string Id { get; set; } = null!;

    public string SubjectId { get; set; } = null!;

    public string PhoneNumber { get; set; } = null!;

    public int PhoneType { get; set; }

    public bool IsPrimary { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public SubjectDb Subject { get; set; } = null!;
}

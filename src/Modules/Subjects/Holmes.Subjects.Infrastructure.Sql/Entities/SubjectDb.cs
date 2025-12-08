using Holmes.Core.Domain.ValueObjects;

namespace Holmes.Subjects.Infrastructure.Sql.Entities;

public class SubjectDb
{
    public string SubjectId { get; set; } = null!;

    public string GivenName { get; set; } = null!;

    public string FamilyName { get; set; } = null!;

    public string? MiddleName { get; set; }

    public DateOnly? DateOfBirth { get; set; }

    public string? Email { get; set; }

    public byte[]? EncryptedSsn { get; set; }

    public string? SsnLast4 { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public string? MergedIntoSubjectId { get; set; }

    public UlidId? MergedBy { get; set; }

    public DateTimeOffset? MergedAt { get; set; }

    public ICollection<SubjectAliasDb> Aliases { get; set; } = new List<SubjectAliasDb>();

    public ICollection<SubjectAddressDb> Addresses { get; set; } = new List<SubjectAddressDb>();

    public ICollection<SubjectEmploymentDb> Employments { get; set; } = new List<SubjectEmploymentDb>();

    public ICollection<SubjectEducationDb> Educations { get; set; } = new List<SubjectEducationDb>();

    public ICollection<SubjectReferenceDb> References { get; set; } = new List<SubjectReferenceDb>();

    public ICollection<SubjectPhoneDb> Phones { get; set; } = new List<SubjectPhoneDb>();
}
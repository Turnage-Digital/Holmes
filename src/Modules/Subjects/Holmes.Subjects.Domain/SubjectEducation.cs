using Holmes.Core.Domain.ValueObjects;

namespace Holmes.Subjects.Domain;

public sealed class SubjectEducation
{
    private SubjectEducation()
    {
    }

    public UlidId Id { get; private set; }

    public string InstitutionName { get; private set; } = null!;

    public string? InstitutionAddress { get; private set; }

    public string? Degree { get; private set; }

    public string? Major { get; private set; }

    public DateOnly? AttendedFrom { get; private set; }

    public DateOnly? AttendedTo { get; private set; }

    public DateOnly? GraduationDate { get; private set; }

    public bool Graduated { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public static SubjectEducation Create(
        UlidId id,
        string institutionName,
        string? institutionAddress,
        string? degree,
        string? major,
        DateOnly? attendedFrom,
        DateOnly? attendedTo,
        DateOnly? graduationDate,
        bool graduated,
        DateTimeOffset createdAt)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(institutionName);

        return new SubjectEducation
        {
            Id = id,
            InstitutionName = institutionName,
            InstitutionAddress = institutionAddress,
            Degree = degree,
            Major = major,
            AttendedFrom = attendedFrom,
            AttendedTo = attendedTo,
            GraduationDate = graduationDate,
            Graduated = graduated,
            CreatedAt = createdAt
        };
    }

    public static SubjectEducation Rehydrate(
        UlidId id,
        string institutionName,
        string? institutionAddress,
        string? degree,
        string? major,
        DateOnly? attendedFrom,
        DateOnly? attendedTo,
        DateOnly? graduationDate,
        bool graduated,
        DateTimeOffset createdAt)
    {
        return new SubjectEducation
        {
            Id = id,
            InstitutionName = institutionName,
            InstitutionAddress = institutionAddress,
            Degree = degree,
            Major = major,
            AttendedFrom = attendedFrom,
            AttendedTo = attendedTo,
            GraduationDate = graduationDate,
            Graduated = graduated,
            CreatedAt = createdAt
        };
    }
}

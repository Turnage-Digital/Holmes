using Holmes.Core.Domain.ValueObjects;

namespace Holmes.Subjects.Domain;

public sealed class SubjectEmployment
{
    private SubjectEmployment()
    {
    }

    public UlidId Id { get; private set; }

    public string EmployerName { get; private set; } = null!;

    public string? EmployerPhone { get; private set; }

    public string? EmployerAddress { get; private set; }

    public string? JobTitle { get; private set; }

    public string? SupervisorName { get; private set; }

    public string? SupervisorPhone { get; private set; }

    public DateOnly StartDate { get; private set; }

    public DateOnly? EndDate { get; private set; }

    public bool IsCurrent => EndDate is null;

    public string? ReasonForLeaving { get; private set; }

    public bool CanContact { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public static SubjectEmployment Create(
        UlidId id,
        string employerName,
        string? employerPhone,
        string? employerAddress,
        string? jobTitle,
        string? supervisorName,
        string? supervisorPhone,
        DateOnly startDate,
        DateOnly? endDate,
        string? reasonForLeaving,
        bool canContact,
        DateTimeOffset createdAt)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(employerName);

        if (endDate.HasValue && endDate.Value < startDate)
        {
            throw new ArgumentException("End date cannot be before start date", nameof(endDate));
        }

        return new SubjectEmployment
        {
            Id = id,
            EmployerName = employerName,
            EmployerPhone = employerPhone,
            EmployerAddress = employerAddress,
            JobTitle = jobTitle,
            SupervisorName = supervisorName,
            SupervisorPhone = supervisorPhone,
            StartDate = startDate,
            EndDate = endDate,
            ReasonForLeaving = reasonForLeaving,
            CanContact = canContact,
            CreatedAt = createdAt
        };
    }

    public static SubjectEmployment Rehydrate(
        UlidId id,
        string employerName,
        string? employerPhone,
        string? employerAddress,
        string? jobTitle,
        string? supervisorName,
        string? supervisorPhone,
        DateOnly startDate,
        DateOnly? endDate,
        string? reasonForLeaving,
        bool canContact,
        DateTimeOffset createdAt)
    {
        return new SubjectEmployment
        {
            Id = id,
            EmployerName = employerName,
            EmployerPhone = employerPhone,
            EmployerAddress = employerAddress,
            JobTitle = jobTitle,
            SupervisorName = supervisorName,
            SupervisorPhone = supervisorPhone,
            StartDate = startDate,
            EndDate = endDate,
            ReasonForLeaving = reasonForLeaving,
            CanContact = canContact,
            CreatedAt = createdAt
        };
    }
}

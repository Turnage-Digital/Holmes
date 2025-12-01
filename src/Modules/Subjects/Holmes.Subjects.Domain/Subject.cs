using System.Collections.ObjectModel;
using Holmes.Core.Domain;
using Holmes.Core.Domain.ValueObjects;
using Holmes.Subjects.Domain.Events;
using MediatR;

namespace Holmes.Subjects.Domain;

public sealed class Subject : AggregateRoot
{
    private readonly List<SubjectAlias> _aliases = [];

    private Subject()
    {
    }

    public UlidId Id { get; private set; }

    public string GivenName { get; private set; } = null!;

    public string FamilyName { get; private set; } = null!;

    public DateOnly? DateOfBirth { get; private set; }

    public string? Email { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public bool IsMerged => MergedIntoSubjectId is not null;

    public UlidId? MergedIntoSubjectId { get; private set; }

    public UlidId? MergedBy { get; private set; }

    public DateTimeOffset? MergedAt { get; private set; }

    public IReadOnlyCollection<SubjectAlias> Aliases => new ReadOnlyCollection<SubjectAlias>(_aliases);

    public static Subject Register(
        UlidId id,
        string givenName,
        string familyName,
        DateOnly? dateOfBirth,
        string? email,
        DateTimeOffset registeredAt
    )
    {
        ArgumentNullException.ThrowIfNull(givenName);
        ArgumentNullException.ThrowIfNull(familyName);

        var subject = new Subject();
        subject.Apply(new SubjectRegistered(
            id,
            givenName,
            familyName,
            dateOfBirth,
            email,
            registeredAt));
        return subject;
    }

    public static Subject Rehydrate(
        UlidId id,
        string givenName,
        string familyName,
        DateOnly? dateOfBirth,
        string? email,
        DateTimeOffset createdAt,
        IEnumerable<SubjectAlias> aliases,
        UlidId? mergedInto,
        UlidId? mergedBy,
        DateTimeOffset? mergedAt
    )
    {
        var subject = new Subject
        {
            Id = id,
            GivenName = givenName,
            FamilyName = familyName,
            DateOfBirth = dateOfBirth,
            Email = email,
            CreatedAt = createdAt,
            MergedIntoSubjectId = mergedInto,
            MergedBy = mergedBy,
            MergedAt = mergedAt
        };

        subject._aliases.AddRange(aliases);
        return subject;
    }

    public void UpdateProfile(string givenName, string familyName, DateOnly? dateOfBirth, string? email)
    {
        ArgumentNullException.ThrowIfNull(givenName);
        ArgumentNullException.ThrowIfNull(familyName);

        if (GivenName == givenName &&
            FamilyName == familyName &&
            DateOfBirth == dateOfBirth &&
            Email == email)
        {
            return;
        }

        GivenName = givenName;
        FamilyName = familyName;
        DateOfBirth = dateOfBirth;
        Email = email;
    }

    public void AddAlias(SubjectAlias alias, DateTimeOffset timestamp, UlidId addedBy)
    {
        ArgumentNullException.ThrowIfNull(alias);

        if (_aliases.Contains(alias))
        {
            return;
        }

        _aliases.Add(alias);
        Emit(new SubjectAliasAdded(Id, alias.GivenName, alias.FamilyName, alias.DateOfBirth, addedBy, timestamp));
    }

    public void MergeInto(UlidId targetSubjectId, UlidId mergedBy, DateTimeOffset mergedAt)
    {
        if (IsMerged)
        {
            if (MergedIntoSubjectId == targetSubjectId)
            {
                return;
            }

            throw new InvalidOperationException($"Subject '{Id}' already merged into '{MergedIntoSubjectId}'.");
        }

        if (targetSubjectId == Id)
        {
            throw new InvalidOperationException("Cannot merge a subject into itself.");
        }

        MergedIntoSubjectId = targetSubjectId;
        MergedBy = mergedBy;
        MergedAt = mergedAt;
        Emit(new SubjectMerged(Id, targetSubjectId, mergedBy, mergedAt));
    }

    private void Apply(SubjectRegistered @event)
    {
        Id = @event.SubjectId;
        GivenName = @event.GivenName;
        FamilyName = @event.FamilyName;
        DateOfBirth = @event.DateOfBirth;
        Email = @event.Email;
        CreatedAt = @event.RegisteredAt;
        Emit(@event);
    }

    private void Emit(INotification @event)
    {
        AddDomainEvent(@event);
    }
}
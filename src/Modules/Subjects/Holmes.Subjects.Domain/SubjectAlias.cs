using Holmes.Core.Domain.ValueObjects;

namespace Holmes.Subjects.Domain;

public sealed class SubjectAlias : ValueObject
{
    public SubjectAlias(string givenName, string familyName, DateOnly? dateOfBirth)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(givenName);
        ArgumentException.ThrowIfNullOrWhiteSpace(familyName);

        GivenName = givenName;
        FamilyName = familyName;
        DateOfBirth = dateOfBirth;
    }

    public string GivenName { get; }

    public string FamilyName { get; }

    public DateOnly? DateOfBirth { get; }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return GivenName;
        yield return FamilyName;
        yield return DateOfBirth;
    }
}
using Holmes.Core.Domain.Specifications;
using Holmes.Subjects.Infrastructure.Sql.Entities;

namespace Holmes.Subjects.Infrastructure.Sql.Specifications;

public sealed class SubjectByEmailSpec : Specification<SubjectDb>
{
    public SubjectByEmailSpec(string email)
    {
        AddCriteria(s => s.Email == email && s.MergedIntoSubjectId == null);
        AddInclude(s => s.Aliases);
        AddInclude(s => s.Addresses);
        AddInclude(s => s.Employments);
        AddInclude(s => s.Educations);
        AddInclude(s => s.References);
        AddInclude(s => s.Phones);
    }
}
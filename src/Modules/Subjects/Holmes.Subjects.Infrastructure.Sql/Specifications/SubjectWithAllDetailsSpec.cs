using Holmes.Core.Domain.Specifications;
using Holmes.Subjects.Infrastructure.Sql.Entities;

namespace Holmes.Subjects.Infrastructure.Sql.Specifications;

public sealed class SubjectWithAllDetailsSpec : Specification<SubjectDb>
{
    public SubjectWithAllDetailsSpec(string subjectId)
    {
        AddCriteria(s => s.SubjectId == subjectId);
        AddInclude(s => s.Aliases);
        AddInclude(s => s.Addresses);
        AddInclude(s => s.Employments);
        AddInclude(s => s.Educations);
        AddInclude(s => s.References);
        AddInclude(s => s.Phones);
    }
}

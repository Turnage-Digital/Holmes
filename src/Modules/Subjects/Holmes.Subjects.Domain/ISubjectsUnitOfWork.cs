using Holmes.Core.Domain;

namespace Holmes.Subjects.Domain;

public interface ISubjectsUnitOfWork : IUnitOfWork
{
    ISubjectRepository Subjects { get; }
}
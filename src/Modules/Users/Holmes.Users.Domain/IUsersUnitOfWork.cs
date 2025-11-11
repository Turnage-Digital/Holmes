using Holmes.Core.Domain;

namespace Holmes.Users.Domain;

public interface IUsersUnitOfWork : IUnitOfWork
{
    IUserRepository Users { get; }
}
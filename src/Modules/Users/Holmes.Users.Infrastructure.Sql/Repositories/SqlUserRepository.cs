using System;
using System.Threading;
using System.Threading.Tasks;
using Holmes.Core.Domain.ValueObjects;
using Holmes.Users.Domain.Users;

namespace Holmes.Users.Infrastructure.Sql.Repositories;

/// <summary>
/// Placeholder repository; will be replaced with event-sourced persistence in future phase.
/// </summary>
public class SqlUserRepository : IUserRepository
{
    public Task<User?> GetByIdAsync(UlidId id, CancellationToken cancellationToken)
    {
        throw new NotImplementedException("User persistence not implemented yet.");
    }

    public Task<User?> GetByExternalIdentityAsync(string issuer, string subject, CancellationToken cancellationToken)
    {
        throw new NotImplementedException("User persistence not implemented yet.");
    }

    public Task AddAsync(User user, CancellationToken cancellationToken)
    {
        throw new NotImplementedException("User persistence not implemented yet.");
    }
}

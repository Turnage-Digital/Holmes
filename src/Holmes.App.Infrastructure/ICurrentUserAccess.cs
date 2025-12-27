using Holmes.Core.Domain.ValueObjects;

namespace Holmes.App.Infrastructure.Security;

public interface ICurrentUserAccess
{
    Task<UlidId> GetUserIdAsync(CancellationToken cancellationToken);

    Task<bool> IsGlobalAdminAsync(CancellationToken cancellationToken);

    Task<IReadOnlyCollection<string>> GetAllowedCustomerIdsAsync(CancellationToken cancellationToken);

    Task<bool> HasCustomerAccessAsync(string customerId, CancellationToken cancellationToken);
}
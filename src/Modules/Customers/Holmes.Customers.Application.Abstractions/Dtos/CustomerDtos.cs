using Holmes.Customers.Domain;

namespace Holmes.Customers.Application.Abstractions.Dtos;

public sealed record CustomerContactDto(
    string Id,
    string Name,
    string Email,
    string? Phone,
    string? Role
);

public sealed record CustomerListItemDto(
    string Id,
    string TenantId,
    string Name,
    CustomerStatus Status,
    string PolicySnapshotId,
    string? BillingEmail,
    IReadOnlyCollection<CustomerContactDto> Contacts,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt
);

public sealed record CustomerAdminDto(
    string UserId,
    string AssignedBy,
    DateTimeOffset AssignedAt
);

public sealed record CustomerDetailDto(
    string Id,
    string TenantId,
    string Name,
    CustomerStatus Status,
    string PolicySnapshotId,
    string? BillingEmail,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    IReadOnlyCollection<CustomerContactDto> Contacts,
    IReadOnlyCollection<CustomerAdminDto> Admins
);
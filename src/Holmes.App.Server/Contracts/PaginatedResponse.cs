using System;

namespace Holmes.App.Server.Contracts;

public sealed record PaginatedResponse<T>(
    IReadOnlyCollection<T> Items,
    int Page,
    int PageSize,
    int TotalItems,
    int TotalPages
)
{
    public static PaginatedResponse<T> Create(
        IReadOnlyCollection<T> items,
        int page,
        int pageSize,
        int totalItems
    )
    {
        if (pageSize <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(pageSize));
        }

        var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);
        return new PaginatedResponse<T>(items, page, pageSize, totalItems, totalPages);
    }
}

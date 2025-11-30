namespace Holmes.App.Server.Contracts;

public static class PaginationNormalization
{
    public static (int Page, int PageSize) Normalize(PaginationQuery query)
    {
        return Normalize(query.Page, query.PageSize);
    }

    public static (int Page, int PageSize) Normalize(int page, int pageSize)
    {
        var currentPage = page <= 0 ? 1 : page;
        var size = pageSize <= 0 ? 25 : Math.Min(pageSize, 100);
        return (currentPage, size);
    }
}

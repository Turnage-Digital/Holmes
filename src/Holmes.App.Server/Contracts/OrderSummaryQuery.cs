namespace Holmes.App.Server.Contracts;

public sealed record OrderSummaryQuery
{
    public int Page { get; init; } = 1;

    public int PageSize { get; init; } = 25;

    public string? CustomerId { get; init; }

    public string? SubjectId { get; init; }

    public string? OrderId { get; init; }

    public IReadOnlyCollection<string>? Status { get; init; }
}
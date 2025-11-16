namespace Holmes.App.Server.Contracts;

public sealed record OrderTimelineQuery
{
    public DateTimeOffset? Before { get; init; }

    public int Limit { get; init; } = 50;
}

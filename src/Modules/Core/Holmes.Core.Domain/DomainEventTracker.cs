using System.Threading;

namespace Holmes.Core.Domain;

public static class DomainEventTracker
{
    private static readonly AsyncLocal<HashSet<IHasDomainEvents>?> Aggregates = new();

    public static void Register(IHasDomainEvents aggregate)
    {
        if (aggregate is null)
        {
            return;
        }

        var set = Aggregates.Value;
        if (set is null)
        {
            set = [];
            Aggregates.Value = set;
        }

        set.Add(aggregate);
    }

    public static IReadOnlyCollection<IHasDomainEvents> Collect()
    {
        var set = Aggregates.Value;
        if (set is null || set.Count == 0)
        {
            return Array.Empty<IHasDomainEvents>();
        }

        var aggregates = set.ToArray();
        set.Clear();
        return aggregates;
    }
}

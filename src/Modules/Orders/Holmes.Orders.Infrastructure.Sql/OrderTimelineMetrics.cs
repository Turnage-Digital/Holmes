using System.Diagnostics.Metrics;

namespace Holmes.Orders.Infrastructure.Sql;

internal static class OrderTimelineMetrics
{
    private static readonly Meter Meter = new("Holmes.Orders.Timeline", "1.0");

    public static readonly Counter<long> EventsWritten =
        Meter.CreateCounter<long>("holmes.timeline.events_written");
}
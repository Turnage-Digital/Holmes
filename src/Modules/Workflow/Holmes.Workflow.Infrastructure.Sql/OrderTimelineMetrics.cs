using System.Diagnostics.Metrics;

namespace Holmes.Workflow.Infrastructure.Sql;

internal static class OrderTimelineMetrics
{
    private static readonly Meter Meter = new("Holmes.Workflow.Timeline", "1.0");

    public static readonly Counter<long> EventsWritten =
        Meter.CreateCounter<long>("holmes.timeline.events_written");
}
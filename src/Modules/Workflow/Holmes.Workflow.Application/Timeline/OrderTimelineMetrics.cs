using System.Diagnostics.Metrics;

namespace Holmes.Workflow.Application.Timeline;

public static class OrderTimelineMetrics
{
    private static readonly Meter Meter = new("Holmes.Workflow.Timeline", "1.0");

    public static readonly Counter<long> EventsWritten =
        Meter.CreateCounter<long>("holmes.timeline.events_written");
}

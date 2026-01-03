using System.Diagnostics.Metrics;

namespace Holmes.IntakeSessions.Application;

internal static class IntakeProjectionMetrics
{
    private static readonly Meter Meter = new("Holmes.IntakeSessions.Projections", "1.0");

    public static readonly Counter<long> ProjectionUpdates =
        Meter.CreateCounter<long>("holmes.intake_sessions.projection_updates");
}
using System.Diagnostics.Metrics;

namespace Holmes.Intake.Application.Projections;

internal static class IntakeProjectionMetrics
{
    private static readonly Meter Meter = new("Holmes.Intake.Projections", "1.0");

    public static readonly Counter<long> ProjectionUpdates =
        Meter.CreateCounter<long>("holmes.intake_sessions.projection_updates");
}

using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace Holmes.Core.Infrastructure.Sql;

public static class UnitOfWorkTelemetry
{
    public const string ActivitySourceName = "Holmes.UnitOfWork";
    public const string MeterName = "Holmes.UnitOfWork";

    public static readonly ActivitySource ActivitySource = new(ActivitySourceName);
    private static readonly Meter Meter = new(MeterName);

    public static readonly Histogram<double> SaveChangesDuration =
        Meter.CreateHistogram<double>(
            "holmes.unit_of_work.save_changes.duration",
            "ms",
            "Latency for UnitOfWork.SaveChangesAsync (includes domain event dispatch).");

    public static readonly Counter<long> SaveChangesFailures =
        Meter.CreateCounter<long>(
            "holmes.unit_of_work.save_changes.failures",
            "Number of UnitOfWork.SaveChangesAsync failures.");
}
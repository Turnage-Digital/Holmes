namespace Holmes.SlaClocks.Domain;

/// <summary>
/// The type of SLA clock being tracked.
/// </summary>
public enum ClockKind
{
    /// <summary>
    /// Tracks time from Invited to IntakeComplete.
    /// Default: 1 business day.
    /// </summary>
    Intake = 1,

    /// <summary>
    /// Tracks time from ReadyForRouting to ReadyForReport.
    /// This is the service fulfillment phase where background check services
    /// (court searches, verifications, etc.) are executed.
    /// Default: 3 business days.
    /// </summary>
    Fulfillment = 2,

    /// <summary>
    /// Tracks end-to-end time from Created to Closed.
    /// Default: 5 business days.
    /// </summary>
    Overall = 3,

    /// <summary>
    /// Future: tenant-defined custom clock types.
    /// </summary>
    Custom = 99
}

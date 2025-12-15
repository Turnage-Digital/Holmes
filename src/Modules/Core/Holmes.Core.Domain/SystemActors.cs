namespace Holmes.Core.Domain;

/// <summary>
/// Well-known system actors for audit trail purposes.
/// Background services and automated processes use these IDs
/// so auditors can identify system-initiated actions.
/// </summary>
public static class SystemActors
{
    /// <summary>
    /// The SLA Clock Watchdog background service that monitors
    /// clock deadlines and marks them at-risk or breached.
    /// </summary>
    public const string SlaClockWatchdog = "SYSTEM:SlaClockWatchdog";

    /// <summary>
    /// The Notification Processing background service that
    /// delivers pending notifications via configured providers.
    /// </summary>
    public const string NotificationProcessor = "SYSTEM:NotificationProcessor";

    /// <summary>
    /// General system actor for automated processes that don't
    /// have a more specific identity.
    /// </summary>
    public const string System = "SYSTEM:Automated";
}

namespace Holmes.Notifications.Domain.ValueObjects;

public sealed record NotificationSchedule
{
    public ScheduleType Type { get; init; }
    public TimeSpan? Delay { get; init; }
    public TimeOnly? DailyAt { get; init; }
    public DayOfWeek[]? DaysOfWeek { get; init; }
    public string? CronExpression { get; init; }
    public string? BatchKey { get; init; }
    public TimeSpan? BatchWindow { get; init; }

    public static NotificationSchedule Immediate()
    {
        return new NotificationSchedule { Type = ScheduleType.Immediate };
    }

    public static NotificationSchedule Delayed(TimeSpan delay)
    {
        return new NotificationSchedule
        {
            Type = ScheduleType.Delayed,
            Delay = delay
        };
    }

    public static NotificationSchedule Daily(TimeOnly at)
    {
        return new NotificationSchedule
        {
            Type = ScheduleType.Daily,
            DailyAt = at
        };
    }

    public static NotificationSchedule Weekly(DayOfWeek[] days, TimeOnly at)
    {
        return new NotificationSchedule
        {
            Type = ScheduleType.Weekly,
            DaysOfWeek = days,
            DailyAt = at
        };
    }

    public static NotificationSchedule Batched(string batchKey, TimeSpan window)
    {
        return new NotificationSchedule
        {
            Type = ScheduleType.Batched,
            BatchKey = batchKey,
            BatchWindow = window
        };
    }
}

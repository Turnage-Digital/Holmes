namespace Holmes.Services.Domain;

public enum ServiceStatus
{
    Pending = 0,
    Dispatched = 1,
    InProgress = 2,
    Completed = 3,
    Failed = 4,
    Canceled = 5
}
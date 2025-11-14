namespace Holmes.Workflow.Domain;

public enum OrderStatus
{
    Created = 0,
    Invited = 1,
    IntakeInProgress = 2,
    IntakeComplete = 3,
    ReadyForRouting = 4,
    RoutingInProgress = 5,
    ReadyForReport = 6,
    Closed = 7,
    Blocked = 8
}

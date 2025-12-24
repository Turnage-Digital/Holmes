namespace Holmes.Orders.Domain;

public enum OrderStatus
{
    Created = 0,
    Invited = 1,
    IntakeInProgress = 2,
    IntakeComplete = 3,
    ReadyForFulfillment = 4,
    FulfillmentInProgress = 5,
    ReadyForReport = 6,
    Closed = 7,
    Blocked = 8,
    Canceled = 9
}
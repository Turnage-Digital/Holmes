namespace Holmes.Intake.Domain;

public enum IntakeSessionStatus
{
    Invited = 0,
    InProgress = 1,
    AwaitingReview = 2,
    Submitted = 3,
    Abandoned = 4
}
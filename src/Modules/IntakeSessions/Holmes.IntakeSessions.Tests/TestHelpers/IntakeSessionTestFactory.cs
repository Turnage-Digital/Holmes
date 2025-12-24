using Holmes.Core.Domain.ValueObjects;
using Holmes.IntakeSessions.Domain;
using Holmes.IntakeSessions.Domain.ValueObjects;

namespace Holmes.IntakeSessions.Tests.TestHelpers;

internal static class IntakeSessionTestFactory
{
    public static IntakeSession CreateInvitedSession()
    {
        var snapshot = PolicySnapshot.Create("snapshot-1", "schema-1", DateTimeOffset.UtcNow);
        return IntakeSession.Invite(
            UlidId.NewUlid(),
            UlidId.NewUlid(),
            UlidId.NewUlid(),
            UlidId.NewUlid(),
            snapshot,
            "resume-token",
            DateTimeOffset.UtcNow,
            TimeSpan.FromDays(7));
    }
}
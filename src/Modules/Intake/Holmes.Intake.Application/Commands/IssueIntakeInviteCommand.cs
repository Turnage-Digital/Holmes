using System.Security.Cryptography;
using Holmes.Core.Application;
using Holmes.Core.Domain.Results;
using Holmes.Core.Domain.ValueObjects;
using Holmes.Intake.Domain;
using Holmes.Intake.Domain.ValueObjects;
using Holmes.Workflow.Application.Commands;
using MediatR;

namespace Holmes.Intake.Application.Commands;

public sealed record IssueIntakeInviteCommand(
    UlidId OrderId,
    UlidId SubjectId,
    UlidId CustomerId,
    string PolicySnapshotId,
    string PolicySnapshotSchemaVersion,
    IReadOnlyDictionary<string, string> PolicyMetadata,
    DateTimeOffset PolicyCapturedAt,
    DateTimeOffset InvitedAt,
    TimeSpan TimeToLive,
    string? ResumeToken
) : RequestBase<Result<IssueIntakeInviteResult>>;

public sealed record IssueIntakeInviteResult(
    UlidId IntakeSessionId,
    string ResumeToken,
    DateTimeOffset ExpiresAt
);

public sealed class IssueIntakeInviteCommandHandler(
    IIntakeUnitOfWork unitOfWork,
    ISender sender
) : IRequestHandler<IssueIntakeInviteCommand, Result<IssueIntakeInviteResult>>
{
    private const int DefaultTimeToLiveHours = 168;

    public async Task<Result<IssueIntakeInviteResult>> Handle(
        IssueIntakeInviteCommand request,
        CancellationToken cancellationToken
    )
    {
        var metadata = request.PolicyMetadata ?? new Dictionary<string, string>();
        var ttl = request.TimeToLive <= TimeSpan.Zero
            ? TimeSpan.FromHours(DefaultTimeToLiveHours)
            : request.TimeToLive;

        var resumeToken = string.IsNullOrWhiteSpace(request.ResumeToken)
            ? GenerateResumeToken()
            : request.ResumeToken!;

        var session = IntakeSession.Invite(
            UlidId.NewUlid(),
            request.OrderId,
            request.SubjectId,
            request.CustomerId,
            PolicySnapshot.Create(
                request.PolicySnapshotId,
                request.PolicySnapshotSchemaVersion,
                request.PolicyCapturedAt,
                metadata),
            resumeToken,
            request.InvitedAt,
            ttl);

        await unitOfWork.IntakeSessions.AddAsync(session, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        await sender.Send(new RecordOrderInviteCommand(
            request.OrderId,
            session.Id,
            request.InvitedAt,
            "Intake invitation issued"), cancellationToken);

        return Result.Success(new IssueIntakeInviteResult(
            session.Id,
            session.ResumeToken,
            session.ExpiresAt));
    }

    private static string GenerateResumeToken()
    {
        Span<byte> bytes = stackalloc byte[32];
        RandomNumberGenerator.Fill(bytes);
        return Convert.ToHexString(bytes);
    }
}
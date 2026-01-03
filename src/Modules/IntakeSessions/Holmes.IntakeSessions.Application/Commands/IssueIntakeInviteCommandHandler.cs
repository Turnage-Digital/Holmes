using System.Security.Cryptography;
using Holmes.Core.Application;
using Holmes.Core.Domain.ValueObjects;
using Holmes.IntakeSessions.Contracts.Services;
using Holmes.IntakeSessions.Domain;
using Holmes.IntakeSessions.Domain.ValueObjects;
using MediatR;

namespace Holmes.IntakeSessions.Application.Commands;

public sealed class IssueIntakeInviteCommandHandler(
    IIntakeSessionsUnitOfWork unitOfWork,
    IIntakeSectionMappingService sectionMappingService
) : IRequestHandler<IssueIntakeInviteCommand, Result<IssueIntakeInviteResult>>
{
    private const int DefaultTimeToLiveHours = 168;
    private const string RequiredSectionsKey = "requiredSections";
    private const string OrderedServicesKey = "orderedServices";

    public async Task<Result<IssueIntakeInviteResult>> Handle(
        IssueIntakeInviteCommand request,
        CancellationToken cancellationToken
    )
    {
        // Build metadata with section requirements computed from ordered services
        var metadata = BuildMetadataWithSections(request);

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

        // IntakeSessionInvited domain event is published by UnitOfWork.SaveChangesAsync
        // IntakeToWorkflowHandler in App.Integration listens and sends RecordOrderInviteCommand

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

    private IReadOnlyDictionary<string, string> BuildMetadataWithSections(IssueIntakeInviteCommand request)
    {
        // Start with the provided metadata
        var metadata = new Dictionary<string, string>(
            request.PolicyMetadata,
            StringComparer.OrdinalIgnoreCase);

        // If ordered services were provided, compute required sections
        if (request.OrderedServiceCodes is { Count: > 0 })
        {
            var requiredSections = sectionMappingService.GetRequiredSections(request.OrderedServiceCodes);
            metadata[RequiredSectionsKey] = string.Join(",", requiredSections);
            metadata[OrderedServicesKey] = string.Join(",", request.OrderedServiceCodes);
        }

        return metadata;
    }
}
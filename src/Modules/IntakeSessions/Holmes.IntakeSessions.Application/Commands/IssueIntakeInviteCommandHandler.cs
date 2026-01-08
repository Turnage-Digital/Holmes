using System.Security.Cryptography;
using System.Text;
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
    private const string DefaultDisclosureVersion = "disclosure-v1";
    private const string DefaultAuthorizationVersion = "authorization-v1";
    private const string DefaultDisclosureFormat = "text";
    private const string DefaultAuthorizationFormat = "text";
    private const string DefaultDisclosureContent =
        "A consumer report (background check) may be obtained about you for employment purposes.\n" +
        "This disclosure is provided before authorization is requested.";
    private const string DefaultDisclosureContentOngoing =
        "A consumer report (background check) may be obtained about you for employment purposes.\n" +
        "Reports may be obtained at any time during your relationship for compliance or monitoring purposes.\n" +
        "This disclosure is provided before authorization is requested.";
    private const string DefaultAuthorizationContent =
        "I authorize Holmes to obtain a consumer report about me for employment purposes.\n" +
        "This authorization applies to this background check request and may be revoked in writing.";
    private const string DefaultAuthorizationContentOngoing =
        "I authorize Holmes to obtain consumer reports about me for employment purposes, including the initial " +
        "background check and additional checks during my relationship for compliance or monitoring purposes.\n" +
        "This authorization may be revoked in writing.";

    public async Task<Result<IssueIntakeInviteResult>> Handle(
        IssueIntakeInviteCommand request,
        CancellationToken cancellationToken
    )
    {
        // Build metadata with section requirements computed from ordered services
        var metadata = BuildMetadataWithSections(request);
        ApplyDisclosureSnapshot(metadata);

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

    private static void ApplyDisclosureSnapshot(IDictionary<string, string> metadata)
    {
        var authorizationMode = NormalizeAuthorizationMode(
            GetValueOrDefault(metadata, IntakeMetadataKeys.AuthorizationMode, AuthorizationModes.OneTime));
        metadata[IntakeMetadataKeys.AuthorizationMode] = authorizationMode;

        var disclosureContent = GetValueOrDefault(
            metadata,
            IntakeMetadataKeys.DisclosureContent,
            authorizationMode == AuthorizationModes.Ongoing
                ? DefaultDisclosureContentOngoing
                : DefaultDisclosureContent);
        disclosureContent = NormalizeContent(disclosureContent);
        metadata[IntakeMetadataKeys.DisclosureContent] = disclosureContent;

        var authorizationContent = GetValueOrDefault(
            metadata,
            IntakeMetadataKeys.AuthorizationContent,
            authorizationMode == AuthorizationModes.Ongoing
                ? DefaultAuthorizationContentOngoing
                : DefaultAuthorizationContent);
        authorizationContent = NormalizeContent(authorizationContent);
        metadata[IntakeMetadataKeys.AuthorizationContent] = authorizationContent;

        SetIfMissing(metadata, IntakeMetadataKeys.DisclosureId, UlidId.NewUlid().ToString());
        SetIfMissing(metadata, IntakeMetadataKeys.DisclosureVersion, DefaultDisclosureVersion);
        SetIfMissing(metadata, IntakeMetadataKeys.DisclosureFormat, DefaultDisclosureFormat);
        SetIfMissing(metadata, IntakeMetadataKeys.DisclosureHash, ComputeHash(disclosureContent));

        SetIfMissing(metadata, IntakeMetadataKeys.AuthorizationId, UlidId.NewUlid().ToString());
        SetIfMissing(metadata, IntakeMetadataKeys.AuthorizationVersion, DefaultAuthorizationVersion);
        SetIfMissing(metadata, IntakeMetadataKeys.AuthorizationFormat, DefaultAuthorizationFormat);
        SetIfMissing(metadata, IntakeMetadataKeys.AuthorizationHash, ComputeHash(authorizationContent));
    }

    private static string NormalizeAuthorizationMode(string mode)
    {
        return mode.Equals(AuthorizationModes.Ongoing, StringComparison.OrdinalIgnoreCase)
            ? AuthorizationModes.Ongoing
            : AuthorizationModes.OneTime;
    }

    private static string NormalizeContent(string content)
    {
        return content.Replace("\r\n", "\n");
    }

    private static string ComputeHash(string content)
    {
        var bytes = Encoding.UTF8.GetBytes(content);
        return Convert.ToHexString(SHA256.HashData(bytes));
    }

    private static void SetIfMissing(
        IDictionary<string, string> metadata,
        string key,
        string value)
    {
        if (!metadata.TryGetValue(key, out var existing) || string.IsNullOrWhiteSpace(existing))
        {
            metadata[key] = value;
        }
    }

    private static string GetValueOrDefault(
        IDictionary<string, string> metadata,
        string key,
        string fallback)
    {
        return metadata.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value)
            ? value
            : fallback;
    }
}

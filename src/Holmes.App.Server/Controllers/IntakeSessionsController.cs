using System.ComponentModel.DataAnnotations;
using System.Net;
using Holmes.App.Infrastructure.Security;
using Holmes.Core.Domain.ValueObjects;
using Holmes.IntakeSessions.Application.Commands;
using Holmes.IntakeSessions.Application.Queries;
using Holmes.IntakeSessions.Domain;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Holmes.App.Server.Controllers;

[ApiController]
[Authorize]
[Route("api/intake/sessions")]
public sealed class IntakeSessionsController(IMediator mediator) : ControllerBase
{
    [HttpPost]
    [Authorize(Policy = AuthorizationPolicies.RequireOps)]
    public async Task<IActionResult> IssueInvite(
        [FromBody] IssueIntakeInviteRequest request,
        CancellationToken cancellationToken
    )
    {
        if (!TryParseUlid(request.OrderId, out var orderId) ||
            !TryParseUlid(request.SubjectId, out var subjectId) ||
            !TryParseUlid(request.CustomerId, out var customerId))
        {
            return BadRequest("Order, subject, and customer ids must be ULIDs.");
        }

        var ttlHours = request.TimeToLiveHours is > 0 ? request.TimeToLiveHours.Value : 168;
        var policySnapshotId = string.IsNullOrWhiteSpace(request.PolicySnapshotId)
            ? "policy-default"
            : request.PolicySnapshotId;
        var policySchemaVersion = string.IsNullOrWhiteSpace(request.PolicySnapshotSchemaVersion)
            ? "v1"
            : request.PolicySnapshotSchemaVersion;
        var command = new IssueIntakeInviteCommand(
            orderId,
            subjectId,
            customerId,
            policySnapshotId,
            policySchemaVersion,
            request.PolicyMetadata ?? new Dictionary<string, string>(),
            request.OrderedServiceCodes,
            request.PolicyCapturedAt ?? DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow,
            TimeSpan.FromHours(ttlHours),
            request.ResumeToken);

        var result = await mediator.Send(command, cancellationToken);
        if (!result.IsSuccess)
        {
            return BadRequest(result.Error);
        }

        return CreatedAtAction(nameof(IssueInvite), new { sessionId = result.Value.IntakeSessionId },
            new IssueIntakeInviteResponse(
                result.Value.IntakeSessionId.ToString(),
                result.Value.ResumeToken,
                result.Value.ExpiresAt));
    }

    [HttpPost("{sessionId}/authorization")]
    [AllowAnonymous]
    public async Task<IActionResult> CaptureAuthorization(
        string sessionId,
        [FromBody] CaptureAuthorizationArtifactRequest request,
        CancellationToken cancellationToken
    )
    {
        if (!TryParseUlid(sessionId, out var parsedSession))
        {
            return BadRequest("Invalid intake session id.");
        }

        byte[] payload;
        try
        {
            payload = Convert.FromBase64String(request.PayloadBase64);
        }
        catch (FormatException)
        {
            return BadRequest("Authorization payload must be base64 encoded.");
        }

        var capturedAt = DateTimeOffset.UtcNow;
        var metadata = BuildAuthorizationMetadata(request);

        var command = new CaptureAuthorizationArtifactCommand(
            parsedSession,
            request.MimeType,
            request.SchemaVersion,
            payload,
            capturedAt,
            metadata);

        var result = await mediator.Send(command, cancellationToken);
        if (!result.IsSuccess)
        {
            return BadRequest(result.Error);
        }

        return Ok(new
        {
            ArtifactId = result.Value.ArtifactId.ToString(),
            result.Value.MimeType,
            result.Value.Length,
            result.Value.Hash,
            result.Value.HashAlgorithm,
            result.Value.SchemaVersion,
            result.Value.CreatedAt
        });
    }

    [HttpPost("{sessionId}/start")]
    [AllowAnonymous]
    public async Task<IActionResult> StartSession(
        string sessionId,
        [FromBody] StartIntakeSessionRequest request,
        CancellationToken cancellationToken
    )
    {
        if (!TryParseUlid(sessionId, out var parsedSession))
        {
            return BadRequest("Invalid intake session id.");
        }

        var command = new StartIntakeSessionCommand(
            parsedSession,
            request.ResumeToken,
            request.StartedAt ?? DateTimeOffset.UtcNow,
            request.DeviceInfo);

        var result = await mediator.Send(command, cancellationToken);
        if (!result.IsSuccess)
        {
            return BadRequest(result.Error);
        }

        return Accepted();
    }

    [HttpPost("{sessionId}/progress")]
    [AllowAnonymous]
    public async Task<IActionResult> SaveProgress(
        string sessionId,
        [FromBody] SaveIntakeProgressRequest request,
        CancellationToken cancellationToken
    )
    {
        if (!TryParseUlid(sessionId, out var parsedSession))
        {
            return BadRequest("Invalid intake session id.");
        }

        var command = new SaveIntakeProgressCommand(
            parsedSession,
            request.ResumeToken,
            request.SchemaVersion,
            request.PayloadHash,
            request.PayloadCipherText,
            request.UpdatedAt ?? DateTimeOffset.UtcNow);

        var result = await mediator.Send(command, cancellationToken);
        if (!result.IsSuccess)
        {
            return BadRequest(result.Error);
        }

        return Accepted();
    }

    [HttpPost("{sessionId}/disclosure/viewed")]
    [AllowAnonymous]
    public async Task<IActionResult> RecordDisclosureViewed(
        string sessionId,
        [FromBody] RecordDisclosureViewedRequest request,
        CancellationToken cancellationToken
    )
    {
        if (!TryParseUlid(sessionId, out var parsedSession))
        {
            return BadRequest("Invalid intake session id.");
        }

        var viewedAt = DateTimeOffset.UtcNow;
        var command = new RecordDisclosureViewedCommand(parsedSession, viewedAt);
        var result = await mediator.Send(command, cancellationToken);
        if (!result.IsSuccess)
        {
            return BadRequest(result.Error);
        }

        return Accepted();
    }

    [HttpPost("{sessionId}/otp/verify")]
    [AllowAnonymous]
    public async Task<IActionResult> VerifyOtpCode(
        string sessionId,
        [FromBody] VerifyOtpRequest request,
        CancellationToken cancellationToken
    )
    {
        if (!TryParseUlid(sessionId, out var parsedSession))
        {
            return BadRequest("Invalid intake session id.");
        }

        var command = new VerifyIntakeSessionOtpCommand(parsedSession, request.Code);
        var result = await mediator.Send(command, cancellationToken);
        if (!result.IsSuccess)
        {
            return BadRequest(result.Error);
        }

        return Ok(new VerifyOtpResponse(true));
    }

    [HttpPost("{sessionId}/submit")]
    [AllowAnonymous]
    public async Task<IActionResult> SubmitIntake(
        string sessionId,
        [FromBody] SubmitIntakeRequest request,
        CancellationToken cancellationToken
    )
    {
        if (!TryParseUlid(sessionId, out var parsedSession))
        {
            return BadRequest("Invalid intake session id.");
        }

        var command = new SubmitIntakeCommand(parsedSession, request.SubmittedAt ?? DateTimeOffset.UtcNow);
        var result = await mediator.Send(command, cancellationToken);
        if (!result.IsSuccess)
        {
            return BadRequest(result.Error);
        }

        return Accepted();
    }

    [HttpPost("{sessionId}/accept")]
    [Authorize(Policy = AuthorizationPolicies.RequireOps)]
    public async Task<IActionResult> AcceptSubmission(
        string sessionId,
        [FromBody] AcceptIntakeSubmissionRequest request,
        CancellationToken cancellationToken
    )
    {
        if (!TryParseUlid(sessionId, out var parsedSession))
        {
            return BadRequest("Invalid intake session id.");
        }

        var command = new AcceptIntakeSubmissionCommand(parsedSession, request.AcceptedAt ?? DateTimeOffset.UtcNow);
        var result = await mediator.Send(command, cancellationToken);
        if (!result.IsSuccess)
        {
            return BadRequest(result.Error);
        }

        return Accepted();
    }

    private static bool TryParseUlid(string value, out UlidId id)
    {
        if (Ulid.TryParse(value, out var parsed))
        {
            id = UlidId.FromUlid(parsed);
            return true;
        }

        id = default;
        return false;
    }

    [HttpGet("{sessionId}/bootstrap")]
    [AllowAnonymous]
    public async Task<IActionResult> GetBootstrap(
        string sessionId,
        [FromQuery] string resumeToken,
        CancellationToken cancellationToken
    )
    {
        if (!TryParseUlid(sessionId, out var parsedSession))
        {
            return BadRequest("Invalid intake session id.");
        }

        if (string.IsNullOrWhiteSpace(resumeToken))
        {
            return BadRequest("Resume token is required.");
        }

        var session = await mediator.Send(new GetIntakeSessionBootstrapQuery(parsedSession, resumeToken),
            cancellationToken);
        if (session is null)
        {
            return Unauthorized("Resume token is invalid or session not found.");
        }

        return Ok(session);
    }

    public sealed record IssueIntakeInviteRequest(
        string OrderId,
        string SubjectId,
        string CustomerId,
        string PolicySnapshotId,
        string? PolicySnapshotSchemaVersion,
        IReadOnlyDictionary<string, string>? PolicyMetadata,
        IReadOnlyList<string>? OrderedServiceCodes,
        int? TimeToLiveHours,
        DateTimeOffset? PolicyCapturedAt,
        string? ResumeToken
    );

    public sealed record IssueIntakeInviteResponse(
        string IntakeSessionId,
        string ResumeToken,
        DateTimeOffset ExpiresAt
    );

    public sealed record StartIntakeSessionRequest(
        [Required] string ResumeToken,
        string? DeviceInfo,
        DateTimeOffset? StartedAt
    );

    public sealed record SaveIntakeProgressRequest(
        [Required] string ResumeToken,
        [Required] string SchemaVersion,
        [Required] string PayloadHash,
        [Required] string PayloadCipherText,
        DateTimeOffset? UpdatedAt
    );

    public sealed record VerifyOtpRequest([Required] string Code);

    public sealed record VerifyOtpResponse(bool Verified);

    public sealed record CaptureAuthorizationArtifactRequest(
        string MimeType,
        string SchemaVersion,
        string PayloadBase64,
        DateTimeOffset? CapturedAt,
        IReadOnlyDictionary<string, string>? Metadata
    );

    public sealed record RecordDisclosureViewedRequest(DateTimeOffset? ViewedAt);

    public sealed record SubmitIntakeRequest(DateTimeOffset? SubmittedAt);

    public sealed record AcceptIntakeSubmissionRequest(DateTimeOffset? AcceptedAt);

    private IReadOnlyDictionary<string, string> BuildAuthorizationMetadata(
        CaptureAuthorizationArtifactRequest request)
    {
        var metadata = new Dictionary<string, string>(
            request.Metadata ?? new Dictionary<string, string>(),
            StringComparer.OrdinalIgnoreCase);

        var ipAddress = GetClientIpAddress(HttpContext);
        if (!string.IsNullOrWhiteSpace(ipAddress))
        {
            metadata[IntakeMetadataKeys.ClientIpAddress] = ipAddress;
        }

        var userAgent = HttpContext.Request.Headers.UserAgent.ToString();
        if (!string.IsNullOrWhiteSpace(userAgent))
        {
            metadata[IntakeMetadataKeys.ClientUserAgent] = userAgent;
        }

        metadata[IntakeMetadataKeys.ServerReceivedAt] = DateTimeOffset.UtcNow.ToString("O");
        if (request.CapturedAt.HasValue)
        {
            metadata[IntakeMetadataKeys.ClientCapturedAt] = request.CapturedAt.Value.ToString("O");
        }

        return metadata;
    }

    private static string? GetClientIpAddress(HttpContext context)
    {
        var remoteIp = context.Connection.RemoteIpAddress;
        var forwardedFor = context.Request.Headers["X-Forwarded-For"].ToString();
        if (!string.IsNullOrWhiteSpace(forwardedFor) && IsTrustedProxy(remoteIp))
        {
            var first = forwardedFor.Split(',')[0].Trim();
            if (IPAddress.TryParse(first, out var forwarded))
            {
                return forwarded.ToString();
            }
        }

        return remoteIp?.ToString();
    }

    private static bool IsTrustedProxy(IPAddress? address)
    {
        if (address is null)
        {
            return false;
        }

        if (IPAddress.IsLoopback(address))
        {
            return true;
        }

        if (address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
        {
            var bytes = address.GetAddressBytes();
            return bytes[0] == 10 ||
                   (bytes[0] == 172 && bytes[1] >= 16 && bytes[1] <= 31) ||
                   (bytes[0] == 192 && bytes[1] == 168);
        }

        return false;
    }
}

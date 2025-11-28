using System.ComponentModel.DataAnnotations;
using Holmes.App.Server.Security;
using Holmes.Core.Domain.ValueObjects;
using Holmes.Intake.Application.Commands;
using Holmes.Intake.Application.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Holmes.App.Server.Controllers;

[ApiController]
[Authorize]
[Route("api/intake/sessions")]
public class IntakeSessionsController(IMediator mediator) : ControllerBase
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
        var command = new IssueIntakeInviteCommand(
            orderId,
            subjectId,
            customerId,
            request.PolicySnapshotId,
            request.PolicySnapshotSchemaVersion,
            request.PolicyMetadata ?? new Dictionary<string, string>(),
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

    [HttpPost("{sessionId}/consent")]
    [AllowAnonymous]
    public async Task<IActionResult> CaptureConsent(
        string sessionId,
        [FromBody] CaptureConsentArtifactRequest request,
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
            return BadRequest("Consent payload must be base64 encoded.");
        }

        var command = new CaptureConsentArtifactCommand(
            parsedSession,
            request.MimeType,
            request.SchemaVersion,
            payload,
            request.CapturedAt ?? DateTimeOffset.UtcNow,
            request.Metadata);

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
        string PolicySnapshotSchemaVersion,
        IReadOnlyDictionary<string, string>? PolicyMetadata,
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

    public sealed record CaptureConsentArtifactRequest(
        string MimeType,
        string SchemaVersion,
        string PayloadBase64,
        DateTimeOffset? CapturedAt,
        IReadOnlyDictionary<string, string>? Metadata
    );

    public sealed record SubmitIntakeRequest(DateTimeOffset? SubmittedAt);

    public sealed record AcceptIntakeSubmissionRequest(DateTimeOffset? AcceptedAt);
}
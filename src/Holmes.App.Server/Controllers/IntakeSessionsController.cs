using Holmes.App.Server.Contracts;
using Holmes.App.Server.Security;
using Holmes.Core.Domain.ValueObjects;
using Holmes.Intake.Application.Commands;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Holmes.App.Server.Controllers;

[ApiController]
[Authorize]
[Route("api/intake/sessions")]
public class IntakeSessionsController(IMediator mediator) : ControllerBase
{
    [HttpPost("{sessionId}/consent")]
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

    [HttpPost("{sessionId}/submit")]
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
}
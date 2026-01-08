using System.Net;
using Holmes.Core.Application;
using Holmes.Core.Domain.ValueObjects;
using Holmes.App.Server.Controllers;
using Holmes.IntakeSessions.Application.Commands;
using Holmes.IntakeSessions.Domain;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace Holmes.App.Server.Tests;

public class IntakeAuthorizationCaptureTests
{
    [Test]
    public async Task CaptureAuthorization_Adds_Server_Metadata()
    {
        var mediator = new Mock<IMediator>();
        CaptureAuthorizationArtifactCommand? captured = null;
        var descriptor = new AuthorizationArtifactDescriptor(
            UlidId.NewUlid(),
            "text/plain",
            12,
            "HASH",
            "SHA256",
            "authorization-v1",
            DateTimeOffset.UtcNow);
        mediator.Setup(m => m.Send(It.IsAny<CaptureAuthorizationArtifactCommand>(), It.IsAny<CancellationToken>()))
            .Callback<CaptureAuthorizationArtifactCommand, CancellationToken>((cmd, _) => captured = cmd)
            .ReturnsAsync(Result.Success(descriptor));

        var controller = new IntakeSessionsController(mediator.Object)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            }
        };

        controller.HttpContext.Connection.RemoteIpAddress = IPAddress.Parse("10.0.0.5");
        controller.HttpContext.Request.Headers["X-Forwarded-For"] = "203.0.113.5, 10.0.0.5";
        controller.HttpContext.Request.Headers.UserAgent = "HolmesIntake/1.0";

        var request = new IntakeSessionsController.CaptureAuthorizationArtifactRequest(
            "text/plain",
            "authorization-v1",
            Convert.ToBase64String([1, 2, 3]),
            DateTimeOffset.UtcNow.AddMinutes(-1),
            null);

        var response = await controller.CaptureAuthorization(UlidId.NewUlid().ToString(), request, CancellationToken.None);

        Assert.That(response, Is.TypeOf<OkObjectResult>());
        Assert.That(captured, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(captured!.Metadata, Does.ContainKey(IntakeMetadataKeys.ClientIpAddress));
            Assert.That(captured.Metadata[IntakeMetadataKeys.ClientIpAddress], Is.EqualTo("203.0.113.5"));
            Assert.That(captured.Metadata[IntakeMetadataKeys.ClientUserAgent], Is.EqualTo("HolmesIntake/1.0"));
            Assert.That(captured.Metadata, Does.ContainKey(IntakeMetadataKeys.ServerReceivedAt));
            Assert.That(captured.Metadata, Does.ContainKey(IntakeMetadataKeys.ClientCapturedAt));
        });
    }
}

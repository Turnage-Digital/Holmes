using Holmes.App.Application.Commands;
using Holmes.Core.Domain;
using Holmes.Core.Domain.ValueObjects;
using Holmes.Subjects.Application.Abstractions.Commands;
using MediatR;
using Moq;

namespace Holmes.App.Server.Tests;

[TestFixture]
public sealed class CreateOrderWithIntakeCommandHandlerTests
{
    [Test]
    public async Task Handle_Fails_When_Email_Is_Missing()
    {
        var sender = new Mock<ISender>(MockBehavior.Strict);
        var handler = new CreateOrderWithIntakeCommandHandler(sender.Object);

        var command = new CreateOrderWithIntakeCommand(
            "",
            "+15551234567",
            UlidId.NewUlid(),
            "policy-v1")
        {
            UserId = UlidId.NewUlid().ToString()
        };

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Error, Does.Contain("email"));
        sender.Verify(
            s => s.Send(It.IsAny<IRequest<Result<RequestSubjectIntakeResult>>>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Test]
    public async Task Handle_Maps_Subject_Intake_Result()
    {
        var sender = new Mock<ISender>(MockBehavior.Strict);
        var handler = new CreateOrderWithIntakeCommandHandler(sender.Object);

        var userId = UlidId.NewUlid().ToString();
        var subjectId = UlidId.NewUlid();
        var orderId = UlidId.NewUlid();

        RequestSubjectIntakeCommand? captured = null;
        sender.Setup(s => s.Send(
                It.IsAny<IRequest<Result<RequestSubjectIntakeResult>>>(),
                It.IsAny<CancellationToken>()))
            .Callback<IRequest<Result<RequestSubjectIntakeResult>>, CancellationToken>(
                (command, _) => captured = (RequestSubjectIntakeCommand)command)
            .ReturnsAsync(Result.Success(new RequestSubjectIntakeResult(subjectId, true, orderId)));

        var command = new CreateOrderWithIntakeCommand(
            "subject@example.com",
            "+15551234567",
            UlidId.NewUlid(),
            "policy-v1")
        {
            UserId = userId
        };

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.That(result.IsSuccess, Is.True, result.Error);
        Assert.That(result.Value.SubjectId, Is.EqualTo(subjectId));
        Assert.That(result.Value.OrderId, Is.EqualTo(orderId));
        Assert.That(result.Value.SubjectWasExisting, Is.True);
        Assert.That(captured, Is.Not.Null);
        Assert.That(captured!.UserId, Is.EqualTo(userId));
        sender.Verify(
            s => s.Send(It.IsAny<IRequest<Result<RequestSubjectIntakeResult>>>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Test]
    public async Task Handle_Propagates_Subject_Intake_Failure()
    {
        var sender = new Mock<ISender>(MockBehavior.Strict);
        var handler = new CreateOrderWithIntakeCommandHandler(sender.Object);

        sender.Setup(s => s.Send(
                It.IsAny<IRequest<Result<RequestSubjectIntakeResult>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Fail<RequestSubjectIntakeResult>("subject intake failed"));

        var command = new CreateOrderWithIntakeCommand(
            "subject@example.com",
            null,
            UlidId.NewUlid(),
            "policy-v1")
        {
            UserId = UlidId.NewUlid().ToString()
        };

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Error, Does.Contain("subject intake failed"));
        sender.Verify(
            s => s.Send(It.IsAny<IRequest<Result<RequestSubjectIntakeResult>>>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }
}

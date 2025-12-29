using Holmes.Notifications.Application.EventHandlers;
using Holmes.Core.Domain;
using Holmes.Core.Domain.ValueObjects;
using Holmes.Customers.Application.Abstractions;
using Holmes.Customers.Application.Abstractions.Dtos;
using Holmes.Customers.Domain;
using Holmes.IntakeSessions.Application.Abstractions.IntegrationEvents;
using Holmes.IntakeSessions.Domain.ValueObjects;
using Holmes.Notifications.Application.Commands;
using Holmes.Notifications.Domain;
using Holmes.Subjects.Application.Abstractions;
using Holmes.Subjects.Application.Abstractions.Dtos;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;

namespace Holmes.App.Server.Tests.EventHandlers;

[TestFixture]
public class IntakeInviteNotificationHandlerTests
{
    [SetUp]
    public void SetUp()
    {
        _senderMock = new Mock<ISender>();
        _subjectQueriesMock = new Mock<ISubjectQueries>();
        _customerQueriesMock = new Mock<ICustomerQueries>();
        _loggerMock = new Mock<ILogger<IntakeInviteNotificationHandler>>();

        var configData = new Dictionary<string, string?>
        {
            ["Intake:BaseUrl"] = "https://intake.test.holmes"
        };
        _configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configData)
            .Build();

        _handler = new IntakeInviteNotificationHandler(
            _senderMock.Object,
            _subjectQueriesMock.Object,
            _customerQueriesMock.Object,
            _configuration,
            _loggerMock.Object);
    }

    private Mock<ISender> _senderMock = null!;
    private Mock<ISubjectQueries> _subjectQueriesMock = null!;
    private Mock<ICustomerQueries> _customerQueriesMock = null!;
    private Mock<ILogger<IntakeInviteNotificationHandler>> _loggerMock = null!;
    private IConfiguration _configuration = null!;
    private IntakeInviteNotificationHandler _handler = null!;

    [Test]
    public async Task Handle_CreatesNotification_WhenSubjectHasEmail()
    {
        // Arrange
        var sessionId = UlidId.NewUlid();
        var orderId = UlidId.NewUlid();
        var subjectId = UlidId.NewUlid();
        var customerId = UlidId.NewUlid();
        var resumeToken = "test-resume-token";
        var invitedAt = DateTimeOffset.UtcNow;
        var expiresAt = invitedAt.AddHours(24);

        var notification = new IntakeSessionInvitedIntegrationEvent(
            sessionId,
            orderId,
            subjectId,
            customerId,
            resumeToken,
            invitedAt,
            expiresAt,
            PolicySnapshot.Create("policy-1", "schema-1", invitedAt));

        var subjectSummary = new SubjectSummaryDto(
            subjectId.ToString(),
            "John",
            "Doe",
            new DateOnly(1990, 1, 1),
            "john.doe@test.com",
            false,
            0,
            DateTimeOffset.UtcNow);

        var customerListItem = new CustomerListItemDto(
            customerId.ToString(),
            "tenant-1",
            "Acme Corp",
            CustomerStatus.Active,
            "policy-1",
            "billing@acme.com",
            [],
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow);

        _subjectQueriesMock
            .Setup(x => x.GetSummaryByIdAsync(subjectId.ToString(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(subjectSummary);

        _customerQueriesMock
            .Setup(x => x.GetListItemByIdAsync(customerId.ToString(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(customerListItem);

        _senderMock
            .Setup(x => x.Send(It.IsAny<CreateNotificationCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(
                new CreateNotificationResult(UlidId.NewUlid(), DateTimeOffset.UtcNow, null)));

        // Act
        await _handler.Handle(notification, CancellationToken.None);

        // Assert
        _senderMock.Verify(
            x => x.Send(
                It.Is<CreateNotificationCommand>(cmd =>
                    cmd.CustomerId == customerId &&
                    cmd.Recipient.Address == "john.doe@test.com" &&
                    cmd.Recipient.DisplayName == "John Doe" &&
                    cmd.Content.TemplateId == "intake-invite-v1" &&
                    cmd.Content.Subject == "Action Required: Complete Your Background Check" &&
                    cmd.Priority == NotificationPriority.High &&
                    cmd.IsAdverseAction == false &&
                    cmd.Content.TemplateData.ContainsKey("IntakeUrl") &&
                    cmd.Content.TemplateData["IntakeUrl"].ToString()!.Contains(sessionId.ToString()) &&
                    cmd.Content.TemplateData["IntakeUrl"].ToString()!.Contains(resumeToken) &&
                    cmd.Content.TemplateData["CustomerName"].ToString() == "Acme Corp" &&
                    cmd.Content.TemplateData["SubjectName"].ToString() == "John Doe"),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Test]
    public async Task Handle_DoesNotSendNotification_WhenSubjectNotFound()
    {
        // Arrange
        var notification = CreateTestNotification();

        _subjectQueriesMock
            .Setup(x => x.GetSummaryByIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((SubjectSummaryDto?)null);

        // Act
        await _handler.Handle(notification, CancellationToken.None);

        // Assert
        _senderMock.Verify(
            x => x.Send(It.IsAny<CreateNotificationCommand>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Test]
    public async Task Handle_DoesNotSendNotification_WhenSubjectHasNoEmail()
    {
        // Arrange
        var notification = CreateTestNotification();

        var subjectSummary = new SubjectSummaryDto(
            notification.SubjectId.ToString(),
            "John",
            "Doe",
            new DateOnly(1990, 1, 1),
            null, // No email
            false,
            0,
            DateTimeOffset.UtcNow);

        _subjectQueriesMock
            .Setup(x => x.GetSummaryByIdAsync(notification.SubjectId.ToString(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(subjectSummary);

        // Act
        await _handler.Handle(notification, CancellationToken.None);

        // Assert
        _senderMock.Verify(
            x => x.Send(It.IsAny<CreateNotificationCommand>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Test]
    public async Task Handle_UsesDefaultCustomerName_WhenCustomerNotFound()
    {
        // Arrange
        var notification = CreateTestNotification();

        var subjectSummary = new SubjectSummaryDto(
            notification.SubjectId.ToString(),
            "Jane",
            "Smith",
            new DateOnly(1985, 6, 15),
            "jane.smith@test.com",
            false,
            0,
            DateTimeOffset.UtcNow);

        _subjectQueriesMock
            .Setup(x => x.GetSummaryByIdAsync(notification.SubjectId.ToString(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(subjectSummary);

        _customerQueriesMock
            .Setup(x => x.GetListItemByIdAsync(notification.CustomerId.ToString(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((CustomerListItemDto?)null);

        _senderMock
            .Setup(x => x.Send(It.IsAny<CreateNotificationCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(
                new CreateNotificationResult(UlidId.NewUlid(), DateTimeOffset.UtcNow, null)));

        // Act
        await _handler.Handle(notification, CancellationToken.None);

        // Assert
        _senderMock.Verify(
            x => x.Send(
                It.Is<CreateNotificationCommand>(cmd =>
                    cmd.Content.TemplateData["CustomerName"].ToString() == "Your employer"),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Test]
    public async Task Handle_BuildsCorrectIntakeUrl_WithBaseUrlAndToken()
    {
        // Arrange
        var sessionId = UlidId.NewUlid();
        var resumeToken = "my-special-token-123";
        var notification = CreateTestNotification(sessionId, resumeToken);

        SetupSubjectAndCustomer(notification);

        _senderMock
            .Setup(x => x.Send(It.IsAny<CreateNotificationCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(
                new CreateNotificationResult(UlidId.NewUlid(), DateTimeOffset.UtcNow, null)));

        // Act
        await _handler.Handle(notification, CancellationToken.None);

        // Assert
        _senderMock.Verify(
            x => x.Send(
                It.Is<CreateNotificationCommand>(cmd =>
                    cmd.Content.TemplateData["IntakeUrl"].ToString() ==
                    $"https://intake.test.holmes/intake/{sessionId}?token={resumeToken}"),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Test]
    public async Task Handle_UsesEmailAsDisplayName_WhenSubjectNameIsEmpty()
    {
        // Arrange
        var notification = CreateTestNotification();

        var subjectSummary = new SubjectSummaryDto(
            notification.SubjectId.ToString(),
            "", // Empty first name
            "", // Empty last name
            null,
            "test@example.com",
            false,
            0,
            DateTimeOffset.UtcNow);

        _subjectQueriesMock
            .Setup(x => x.GetSummaryByIdAsync(notification.SubjectId.ToString(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(subjectSummary);

        SetupDefaultCustomer(notification.CustomerId);

        _senderMock
            .Setup(x => x.Send(It.IsAny<CreateNotificationCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(
                new CreateNotificationResult(UlidId.NewUlid(), DateTimeOffset.UtcNow, null)));

        // Act
        await _handler.Handle(notification, CancellationToken.None);

        // Assert
        _senderMock.Verify(
            x => x.Send(
                It.Is<CreateNotificationCommand>(cmd =>
                    cmd.Recipient.DisplayName == "test@example.com" &&
                    cmd.Content.TemplateData["SubjectName"].ToString() == "test@example.com"),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Test]
    public async Task Handle_IncludesExpiresAtInTemplateData()
    {
        // Arrange
        var expiresAt = new DateTimeOffset(2025, 12, 20, 14, 30, 0, TimeSpan.Zero);
        var notification = CreateTestNotification(expiresAt: expiresAt);

        SetupSubjectAndCustomer(notification);

        _senderMock
            .Setup(x => x.Send(It.IsAny<CreateNotificationCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(
                new CreateNotificationResult(UlidId.NewUlid(), DateTimeOffset.UtcNow, null)));

        // Act
        await _handler.Handle(notification, CancellationToken.None);

        // Assert
        _senderMock.Verify(
            x => x.Send(
                It.Is<CreateNotificationCommand>(cmd =>
                    cmd.Content.TemplateData.ContainsKey("ExpiresAt") &&
                    cmd.Content.TemplateData.ContainsKey("ExpiresAtUtc")),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    private static IntakeSessionInvitedIntegrationEvent CreateTestNotification(
        UlidId? sessionId = null,
        string? resumeToken = null,
        DateTimeOffset? expiresAt = null
    )
    {
        var invitedAt = DateTimeOffset.UtcNow;
        return new IntakeSessionInvitedIntegrationEvent(
            sessionId ?? UlidId.NewUlid(),
            UlidId.NewUlid(),
            UlidId.NewUlid(),
            UlidId.NewUlid(),
            resumeToken ?? "test-token",
            invitedAt,
            expiresAt ?? invitedAt.AddHours(24),
            PolicySnapshot.Create("policy-1", "schema-1", invitedAt));
    }

    private void SetupSubjectAndCustomer(IntakeSessionInvitedIntegrationEvent notification)
    {
        var subjectSummary = new SubjectSummaryDto(
            notification.SubjectId.ToString(),
            "Test",
            "User",
            new DateOnly(1990, 1, 1),
            "test.user@test.com",
            false,
            0,
            DateTimeOffset.UtcNow);

        _subjectQueriesMock
            .Setup(x => x.GetSummaryByIdAsync(notification.SubjectId.ToString(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(subjectSummary);

        SetupDefaultCustomer(notification.CustomerId);
    }

    private void SetupDefaultCustomer(UlidId customerId)
    {
        var customer = new CustomerListItemDto(
            customerId.ToString(),
            "tenant-1",
            "Test Company",
            CustomerStatus.Active,
            "policy-1",
            "billing@test.com",
            [],
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow);

        _customerQueriesMock
            .Setup(x => x.GetListItemByIdAsync(customerId.ToString(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(customer);
    }
}

using Holmes.Core.Domain.ValueObjects;
using Holmes.IntakeSessions.Application.Commands;
using Holmes.IntakeSessions.Contracts.Services;
using Holmes.IntakeSessions.Domain;
using Moq;

namespace Holmes.IntakeSessions.Tests;

public class IssueIntakeInviteCommandTests
{
    private Mock<IIntakeSessionRepository> _repositoryMock = null!;
    private Mock<IIntakeSectionMappingService> _sectionMappingServiceMock = null!;
    private Mock<IIntakeSessionsUnitOfWork> _unitOfWorkMock = null!;

    [SetUp]
    public void SetUp()
    {
        _repositoryMock = new Mock<IIntakeSessionRepository>();
        _unitOfWorkMock = new Mock<IIntakeSessionsUnitOfWork>();
        _unitOfWorkMock.Setup(x => x.IntakeSessions).Returns(_repositoryMock.Object);
        _sectionMappingServiceMock = new Mock<IIntakeSectionMappingService>();
        _sectionMappingServiceMock
            .Setup(x => x.GetRequiredSections(It.IsAny<IEnumerable<string>>()))
            .Returns(new HashSet<string>());
    }

    [Test]
    public async Task CreatesSession()
    {
        var handler = new IssueIntakeInviteCommandHandler(
            _unitOfWorkMock.Object,
            _sectionMappingServiceMock.Object);
        var command = new IssueIntakeInviteCommand(
            UlidId.NewUlid(),
            UlidId.NewUlid(),
            UlidId.NewUlid(),
            "policy",
            "schema",
            new Dictionary<string, string>(),
            null, // OrderedServiceCodes
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow,
            TimeSpan.FromHours(24),
            null);

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.That(result.IsSuccess, Is.True);
        _repositoryMock.Verify(x => x.AddAsync(It.IsAny<IntakeSession>(), It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        // Note: Workflow notification now happens via IntakeSessionInvitedIntegrationEvent
        // handled by Orders.Application.
    }
}
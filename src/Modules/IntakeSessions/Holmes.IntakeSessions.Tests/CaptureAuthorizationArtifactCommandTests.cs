using Holmes.Core.Domain.ValueObjects;
using Holmes.IntakeSessions.Application.Commands;
using Holmes.IntakeSessions.Domain;
using Holmes.IntakeSessions.Domain.ValueObjects;
using Holmes.IntakeSessions.Tests.TestHelpers;
using Moq;

namespace Holmes.IntakeSessions.Tests;

public class CaptureAuthorizationArtifactCommandTests
{
    private InMemoryIntakeSessionRepository _repository = null!;
    private Mock<IAuthorizationArtifactStore> _storeMock = null!;
    private Mock<IIntakeSessionsUnitOfWork> _unitOfWorkMock = null!;

    [SetUp]
    public void SetUp()
    {
        _repository = new InMemoryIntakeSessionRepository();
        _storeMock = new Mock<IAuthorizationArtifactStore>();
        _unitOfWorkMock = new Mock<IIntakeSessionsUnitOfWork>();
        _unitOfWorkMock.Setup(x => x.IntakeSessions).Returns(_repository);
    }

    [Test]
    public async Task PersistsArtifactAndUpdatesSession()
    {
        var policyMetadata = new Dictionary<string, string>
        {
            { IntakeMetadataKeys.DisclosureId, "disclosure-1" },
            { IntakeMetadataKeys.DisclosureVersion, "disclosure-v1" },
            { IntakeMetadataKeys.DisclosureHash, "DISCLOSUREHASH" },
            { IntakeMetadataKeys.DisclosureFormat, "text" },
            { IntakeMetadataKeys.AuthorizationId, "authorization-1" },
            { IntakeMetadataKeys.AuthorizationVersion, "authorization-v1" },
            { IntakeMetadataKeys.AuthorizationHash, "AUTHHASH" },
            { IntakeMetadataKeys.AuthorizationFormat, "text" },
            { IntakeMetadataKeys.AuthorizationMode, AuthorizationModes.OneTime }
        };
        var snapshot = PolicySnapshot.Create("snapshot-1", "schema-1", DateTimeOffset.UtcNow, policyMetadata);
        var session = IntakeSession.Invite(
            UlidId.NewUlid(),
            UlidId.NewUlid(),
            UlidId.NewUlid(),
            UlidId.NewUlid(),
            snapshot,
            "resume-token",
            DateTimeOffset.UtcNow,
            TimeSpan.FromHours(24));
        session.Start(DateTimeOffset.UtcNow, null);
        await _repository.AddAsync(session, CancellationToken.None);

        var descriptor = new AuthorizationArtifactDescriptor(
            UlidId.NewUlid(),
            "application/pdf",
            1024,
            "hash",
            "SHA256",
            "schema",
            DateTimeOffset.UtcNow);
        AuthorizationArtifactWriteRequest? capturedRequest = null;
        _storeMock.Setup(x =>
                x.SaveAsync(It.IsAny<AuthorizationArtifactWriteRequest>(), It.IsAny<Stream>(),
                    It.IsAny<CancellationToken>()))
            .Callback<AuthorizationArtifactWriteRequest, Stream, CancellationToken>((req, _, _) =>
                capturedRequest = req)
            .ReturnsAsync(descriptor);

        var handler = new CaptureAuthorizationArtifactCommandHandler(_storeMock.Object, _unitOfWorkMock.Object);
        var command = new CaptureAuthorizationArtifactCommand(
            session.Id,
            "application/pdf",
            "schema",
            [1, 2, 3],
            DateTimeOffset.UtcNow,
            new Dictionary<string, string>
            {
                { IntakeMetadataKeys.ClientIpAddress, "203.0.113.10" },
                { IntakeMetadataKeys.ClientUserAgent, "HolmesIntake/1.0" }
            });

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.Value, Is.EqualTo(descriptor));
            Assert.That(session.AuthorizationArtifact, Is.Not.Null);
            Assert.That(capturedRequest, Is.Not.Null);
        });
        Assert.Multiple(() =>
        {
            Assert.That(capturedRequest!.Metadata[IntakeMetadataKeys.DisclosureId], Is.EqualTo("disclosure-1"));
            Assert.That(capturedRequest.Metadata[IntakeMetadataKeys.DisclosureVersion], Is.EqualTo("disclosure-v1"));
            Assert.That(capturedRequest.Metadata[IntakeMetadataKeys.DisclosureHash], Is.EqualTo("DISCLOSUREHASH"));
            Assert.That(capturedRequest.Metadata[IntakeMetadataKeys.AuthorizationMode], Is.EqualTo("one_time"));
            Assert.That(capturedRequest.Metadata[IntakeMetadataKeys.ClientIpAddress], Is.EqualTo("203.0.113.10"));
            Assert.That(capturedRequest.Metadata[IntakeMetadataKeys.ClientUserAgent], Is.EqualTo("HolmesIntake/1.0"));
        });
        _storeMock.Verify(
            x => x.SaveAsync(It.IsAny<AuthorizationArtifactWriteRequest>(), It.IsAny<Stream>(),
                It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    private sealed class InMemoryIntakeSessionRepository : IIntakeSessionRepository
    {
        private readonly Dictionary<UlidId, IntakeSession> _sessions = new();

        public Task AddAsync(IntakeSession session, CancellationToken cancellationToken)
        {
            _sessions[session.Id] = session;
            return Task.CompletedTask;
        }

        public Task<IntakeSession?> GetByIdAsync(UlidId id, CancellationToken cancellationToken)
        {
            _sessions.TryGetValue(id, out var session);
            return Task.FromResult(session);
        }

        public Task<IntakeSession?> GetByOrderIdAsync(UlidId orderId, CancellationToken cancellationToken)
        {
            var session = _sessions.Values
                .OrderByDescending(s => s.CreatedAt)
                .FirstOrDefault(s => s.OrderId == orderId);
            return Task.FromResult(session);
        }

        public Task UpdateAsync(IntakeSession session, CancellationToken cancellationToken)
        {
            _sessions[session.Id] = session;
            return Task.CompletedTask;
        }
    }
}

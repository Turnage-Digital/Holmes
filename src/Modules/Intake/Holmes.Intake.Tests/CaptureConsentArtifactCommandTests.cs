using Holmes.Core.Domain.ValueObjects;
using Holmes.Intake.Application.Commands;
using Holmes.Intake.Domain;
using Holmes.Intake.Domain.Storage;
using Holmes.Intake.Tests.TestHelpers;
using NSubstitute;
using Xunit;

namespace Holmes.Intake.Tests;

public class CaptureConsentArtifactCommandTests
{
    private readonly InMemoryIntakeSessionRepository _repository = new();
    private readonly IConsentArtifactStore _store = Substitute.For<IConsentArtifactStore>();
    private readonly IIntakeUnitOfWork _unitOfWork = Substitute.For<IIntakeUnitOfWork>();

    public CaptureConsentArtifactCommandTests()
    {
        _unitOfWork.IntakeSessions.Returns(_repository);
    }

    [Fact]
    public async Task PersistsArtifactAndUpdatesSession()
    {
        var session = IntakeSessionTestFactory.CreateInvitedSession();
        session.Start(DateTimeOffset.UtcNow, null);
        await _repository.AddAsync(session, CancellationToken.None);

        var descriptor = new ConsentArtifactDescriptor(
            UlidId.NewUlid(),
            "application/pdf",
            1024,
            "hash",
            "SHA256",
            "schema",
            DateTimeOffset.UtcNow);
        _store.SaveAsync(Arg.Any<ConsentArtifactWriteRequest>(), Arg.Any<Stream>(), Arg.Any<CancellationToken>())
            .Returns(descriptor);

        var handler = new CaptureConsentArtifactCommandHandler(_store, _unitOfWork);
        var command = new CaptureConsentArtifactCommand(
            session.Id,
            "application/pdf",
            "schema",
            new byte[] { 1, 2, 3 },
            DateTimeOffset.UtcNow);

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(descriptor, result.Value);
        Assert.NotNull(session.ConsentArtifact);
        await _store.Received(1)
            .SaveAsync(Arg.Any<ConsentArtifactWriteRequest>(), Arg.Any<Stream>(), Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
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

        public Task UpdateAsync(IntakeSession session, CancellationToken cancellationToken)
        {
            _sessions[session.Id] = session;
            return Task.CompletedTask;
        }
    }
}
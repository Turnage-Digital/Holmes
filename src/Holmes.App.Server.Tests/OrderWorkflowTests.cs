using Holmes.Core.Domain.ValueObjects;
using Holmes.Customers.Contracts;
using Holmes.Customers.Contracts.Dtos;
using Holmes.IntakeSessions.Application.EventHandlers;
using Holmes.IntakeSessions.Contracts.IntegrationEvents;
using Holmes.IntakeSessions.Infrastructure.Sql;
using Holmes.Orders.Application.Commands;
using Holmes.Orders.Application.EventHandlers;
using Holmes.Orders.Contracts;
using Holmes.Orders.Contracts.Dtos;
using Holmes.Orders.Contracts.IntegrationEvents;
using Holmes.Orders.Domain;
using Holmes.Orders.Infrastructure.Sql;
using Holmes.Subjects.Application.EventHandlers;
using Holmes.Subjects.Contracts.IntegrationEvents;
using Holmes.Subjects.Infrastructure.Sql;
using Holmes.Users.Contracts;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace Holmes.App.Server.Tests;

[TestFixture]
public class OrderWorkflowTests
{
    [Test]
    public async Task OrderWorkflow_Progresses_And_Is_Idempotent()
    {
        await using var ordersDb = CreateOrdersDb();
        await using var subjectsDb = CreateSubjectsDb();
        await using var intakeDb = CreateIntakeDb();

        var mediator = new Mock<IMediator>();
        var ordersUow = new OrdersUnitOfWork(ordersDb, mediator.Object);
        var subjectsUow = new SubjectsUnitOfWork(subjectsDb, mediator.Object);
        var intakeUow = new IntakeSessionsUnitOfWork(intakeDb, mediator.Object);

        var customerId = UlidId.NewUlid();
        var userId = UlidId.NewUlid();

        var createHandler = new CreateOrderCommandHandler(
            ordersUow,
            new FakeCustomerQueries(customerId.ToString()),
            new FakeUserAccessQueries(true),
            new FakeCustomerAccessQueries([customerId.ToString()]));

        var command = new CreateOrderCommand(
            customerId,
            "policy-v1",
            "workflow@example.com",
            "+15551234567",
            null,
            DateTimeOffset.UtcNow)
        {
            UserId = userId.ToString()
        };

        var createResult = await createHandler.Handle(command, CancellationToken.None);
        Assert.That(createResult.IsSuccess, Is.True);

        var orderId = UlidId.Parse(createResult.Value.OrderId);
        var createdOrder = await ordersUow.Orders.GetByIdAsync(orderId, CancellationToken.None);
        Assert.That(createdOrder, Is.Not.Null);
        Assert.That(createdOrder!.SubjectId, Is.Null);
        Assert.That(createdOrder.SubjectEmail, Is.EqualTo("workflow@example.com"));

        var subjectHandler = new OrderRequestedSubjectHandler(subjectsUow);
        var requestedEvent = new OrderRequestedIntegrationEvent(
            createdOrder.Id,
            createdOrder.CustomerId,
            createdOrder.SubjectEmail,
            createdOrder.SubjectPhone,
            createdOrder.PolicySnapshotId,
            createdOrder.PackageCode,
            createdOrder.CreatedAt,
            userId);

        await subjectHandler.Handle(requestedEvent, CancellationToken.None);

        var subject = await subjectsUow.Subjects.GetByEmailAsync(
            createdOrder.SubjectEmail,
            CancellationToken.None);
        Assert.That(subject, Is.Not.Null);

        var subjectResolvedHandler = new SubjectResolvedOrderHandler(
            ordersUow,
            NullLogger<SubjectResolvedOrderHandler>.Instance);
        var subjectResolvedEvent = new SubjectResolvedIntegrationEvent(
            createdOrder.Id,
            createdOrder.CustomerId,
            subject!.Id,
            DateTimeOffset.UtcNow,
            false);

        await subjectResolvedHandler.Handle(subjectResolvedEvent, CancellationToken.None);

        var resolvedOrder = await ordersUow.Orders.GetByIdAsync(orderId, CancellationToken.None);
        Assert.That(resolvedOrder!.SubjectId, Is.EqualTo(subject.Id));

        var orderQueries = new FakeOrderQueries(resolvedOrder);
        var intakeHandler = new OrderSubjectAssignedIntakeHandler(
            intakeUow,
            orderQueries,
            NullLogger<OrderSubjectAssignedIntakeHandler>.Instance);
        var subjectAssignedEvent = new OrderSubjectAssignedIntegrationEvent(
            resolvedOrder.Id,
            resolvedOrder.CustomerId,
            subject.Id,
            DateTimeOffset.UtcNow);

        await intakeHandler.Handle(subjectAssignedEvent, CancellationToken.None);

        var sessions = await intakeDb.IntakeSessions
            .Where(s => s.OrderId == resolvedOrder.Id.ToString())
            .ToListAsync();
        Assert.That(sessions, Has.Count.EqualTo(1));
        var sessionId = UlidId.Parse(sessions[0].IntakeSessionId);

        var intakeStartedHandler = new IntakeSessionOrderHandler(
            ordersUow,
            NullLogger<IntakeSessionOrderHandler>.Instance);
        await intakeStartedHandler.Handle(new IntakeSessionStartedIntegrationEvent(
            resolvedOrder.Id,
            sessionId,
            DateTimeOffset.UtcNow), CancellationToken.None);

        var invitedOrder = await ordersUow.Orders.GetByIdAsync(orderId, CancellationToken.None);
        Assert.That(invitedOrder!.ActiveIntakeSessionId, Is.EqualTo(sessionId));
        Assert.That(invitedOrder.Status, Is.EqualTo(OrderStatus.Invited));

        var intakeSubmissionHandler = new IntakeSubmissionOrderHandler(
            ordersUow,
            NullLogger<IntakeSubmissionOrderHandler>.Instance);
        await intakeSubmissionHandler.Handle(new IntakeSubmittedIntegrationEvent(
            invitedOrder.Id,
            sessionId,
            DateTimeOffset.UtcNow), CancellationToken.None);

        var submittedOrder = await ordersUow.Orders.GetByIdAsync(orderId, CancellationToken.None);
        Assert.That(submittedOrder!.Status, Is.EqualTo(OrderStatus.IntakeComplete));

        await intakeHandler.Handle(subjectAssignedEvent, CancellationToken.None);
        var replayedSessions = await intakeDb.IntakeSessions
            .Where(s => s.OrderId == resolvedOrder.Id.ToString())
            .ToListAsync();
        Assert.That(replayedSessions, Has.Count.EqualTo(1));

        await intakeSubmissionHandler.Handle(new IntakeSubmittedIntegrationEvent(
            invitedOrder.Id,
            sessionId,
            DateTimeOffset.UtcNow), CancellationToken.None);
        var replayedOrder = await ordersUow.Orders.GetByIdAsync(orderId, CancellationToken.None);
        Assert.That(replayedOrder!.Status, Is.EqualTo(OrderStatus.IntakeComplete));
    }

    private static OrdersDbContext CreateOrdersDb()
    {
        var options = new DbContextOptionsBuilder<OrdersDbContext>()
            .UseInMemoryDatabase($"orders-{Guid.NewGuid()}")
            .Options;
        return new OrdersDbContext(options);
    }

    private static SubjectsDbContext CreateSubjectsDb()
    {
        var options = new DbContextOptionsBuilder<SubjectsDbContext>()
            .UseInMemoryDatabase($"subjects-{Guid.NewGuid()}")
            .Options;
        return new SubjectsDbContext(options);
    }

    private static IntakeSessionsDbContext CreateIntakeDb()
    {
        var options = new DbContextOptionsBuilder<IntakeSessionsDbContext>()
            .UseInMemoryDatabase($"intake-{Guid.NewGuid()}")
            .Options;
        return new IntakeSessionsDbContext(options);
    }

    private sealed class FakeOrderQueries : IOrderQueries
    {
        private readonly Order _order;

        public FakeOrderQueries(Order order)
        {
            _order = order;
        }

        public Task<OrderSummaryDto?> GetSummaryByIdAsync(string orderId, CancellationToken cancellationToken)
        {
            if (!string.Equals(orderId, _order.Id.ToString(), StringComparison.Ordinal))
            {
                return Task.FromResult<OrderSummaryDto?>(null);
            }

            return Task.FromResult<OrderSummaryDto?>(new OrderSummaryDto(
                _order.Id.ToString(),
                _order.SubjectId?.ToString(),
                _order.CustomerId.ToString(),
                _order.PolicySnapshotId,
                _order.PackageCode,
                _order.Status.ToString(),
                _order.LastStatusReason,
                _order.LastUpdatedAt,
                _order.ReadyForFulfillmentAt,
                _order.ClosedAt,
                _order.CanceledAt));
        }

        public Task<OrderSummaryPagedResult> GetSummariesPagedAsync(
            OrderSummaryFilter filter,
            int page,
            int pageSize,
            CancellationToken cancellationToken
        )
        {
            return Task.FromResult(new OrderSummaryPagedResult([], 0));
        }

        public Task<OrderStatsDto> GetStatsAsync(
            IReadOnlyCollection<string>? allowedCustomerIds,
            CancellationToken cancellationToken
        )
        {
            return Task.FromResult(new OrderStatsDto(0, 0, 0, 0, 0, 0));
        }

        public Task<IReadOnlyList<OrderTimelineEntryDto>> GetTimelineAsync(
            string orderId,
            DateTimeOffset? before,
            int limit,
            CancellationToken cancellationToken
        )
        {
            return Task.FromResult<IReadOnlyList<OrderTimelineEntryDto>>([]);
        }

        public Task<string?> GetCustomerIdAsync(string orderId, CancellationToken cancellationToken)
        {
            return Task.FromResult(orderId == _order.Id.ToString() ? _order.CustomerId.ToString() : null);
        }
    }

    private sealed class FakeCustomerQueries : ICustomerQueries
    {
        private readonly string _customerId;

        public FakeCustomerQueries(string customerId)
        {
            _customerId = customerId;
        }

        public Task<CustomerPagedResult> GetCustomersPagedAsync(
            IReadOnlyCollection<string>? allowedCustomerIds,
            int page,
            int pageSize,
            CancellationToken cancellationToken
        )
        {
            return Task.FromResult(new CustomerPagedResult([], 0));
        }

        public Task<CustomerDetailDto?> GetByIdAsync(string customerId, CancellationToken cancellationToken)
        {
            return Task.FromResult<CustomerDetailDto?>(null);
        }

        public Task<CustomerListItemDto?> GetListItemByIdAsync(string customerId, CancellationToken cancellationToken)
        {
            return Task.FromResult<CustomerListItemDto?>(null);
        }

        public Task<bool> ExistsAsync(string customerId, CancellationToken cancellationToken)
        {
            return Task.FromResult(string.Equals(customerId, _customerId, StringComparison.Ordinal));
        }

        public Task<IReadOnlyList<CustomerAdminDto>> GetAdminsAsync(
            string customerId,
            CancellationToken cancellationToken
        )
        {
            return Task.FromResult<IReadOnlyList<CustomerAdminDto>>([]);
        }
    }

    private sealed class FakeUserAccessQueries : IUserAccessQueries
    {
        private readonly bool _isGlobalAdmin;

        public FakeUserAccessQueries(bool isGlobalAdmin)
        {
            _isGlobalAdmin = isGlobalAdmin;
        }

        public Task<bool> IsGlobalAdminAsync(UlidId userId, CancellationToken cancellationToken)
        {
            return Task.FromResult(_isGlobalAdmin);
        }

        public Task<IReadOnlyList<string>> GetGlobalRolesAsync(UlidId userId, CancellationToken cancellationToken)
        {
            return Task.FromResult<IReadOnlyList<string>>([]);
        }
    }

    private sealed class FakeCustomerAccessQueries : ICustomerAccessQueries
    {
        private readonly IReadOnlyCollection<string> _allowedCustomerIds;

        public FakeCustomerAccessQueries(IReadOnlyCollection<string> allowedCustomerIds)
        {
            _allowedCustomerIds = allowedCustomerIds;
        }

        public Task<IReadOnlyCollection<string>> GetAdminCustomerIdsAsync(
            UlidId userId,
            CancellationToken cancellationToken
        )
        {
            return Task.FromResult(_allowedCustomerIds);
        }
    }
}

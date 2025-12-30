# Integration Event Index

This map lists integration event contracts and where they are published and handled.
Update this file when adding or removing integration events.

Conventions
- Contracts live in `...Contracts/IntegrationEvents/`.
- Publishers live in `...Application/EventHandlers/` and publish `*IntegrationEvent` messages.
- Consumers live in `...Application/EventHandlers/` and implement `INotificationHandler<TIntegrationEvent>`.
- When you need outbox delivery, call `SaveChangesAsync(true)` so events are persisted and dispatched by
  `src/Holmes.App.Server/Services/DeferredDispatchProcessor.cs`.

## IntakeSessions

- IntakeSessionInvitedIntegrationEvent
  - Contract: `src/Modules/IntakeSessions/Holmes.IntakeSessions.Contracts/IntegrationEvents/IntakeSessionInvitedIntegrationEvent.cs`
  - Publisher: `src/Modules/IntakeSessions/Holmes.IntakeSessions.Application/EventHandlers/IntakeSessionIntegrationEventPublisher.cs`
  - Consumers: `src/Modules/Orders/Holmes.Orders.Application/EventHandlers/IntakeToWorkflowHandler.cs`;
    `src/Modules/Notifications/Holmes.Notifications.Application/EventHandlers/IntakeInviteNotificationHandler.cs`

- IntakeSessionStartedIntegrationEvent
  - Contract: `src/Modules/IntakeSessions/Holmes.IntakeSessions.Contracts/IntegrationEvents/IntakeSessionStartedIntegrationEvent.cs`
  - Publisher: `src/Modules/IntakeSessions/Holmes.IntakeSessions.Application/EventHandlers/IntakeSessionIntegrationEventPublisher.cs`
  - Consumers: `src/Modules/Orders/Holmes.Orders.Application/EventHandlers/IntakeToWorkflowHandler.cs`

- IntakeSubmissionAcceptedIntegrationEvent
  - Contract: `src/Modules/IntakeSessions/Holmes.IntakeSessions.Contracts/IntegrationEvents/IntakeSubmissionAcceptedIntegrationEvent.cs`
  - Publisher: `src/Modules/IntakeSessions/Holmes.IntakeSessions.Application/EventHandlers/IntakeSessionIntegrationEventPublisher.cs`
  - Consumers: `src/Modules/Orders/Holmes.Orders.Application/EventHandlers/IntakeSubmissionWorkflowHandler.cs`

- IntakeSubmissionReceivedIntegrationEvent
  - Contract: `src/Modules/IntakeSessions/Holmes.IntakeSessions.Contracts/IntegrationEvents/IntakeSubmissionReceivedIntegrationEvent.cs`
  - Publisher: `src/Modules/IntakeSessions/Holmes.IntakeSessions.Application/EventHandlers/IntakeSessionIntegrationEventPublisher.cs`
  - Consumers: `src/Modules/Orders/Holmes.Orders.Application/EventHandlers/IntakeSubmissionWorkflowHandler.cs`

- SubjectIntakeDataCapturedIntegrationEvent
  - Contract: `src/Modules/IntakeSessions/Holmes.IntakeSessions.Contracts/IntegrationEvents/SubjectIntakeDataCapturedIntegrationEvent.cs`
  - Publisher: `src/Modules/IntakeSessions/Holmes.IntakeSessions.Application/EventHandlers/IntakeSessionIntegrationEventPublisher.cs`
  - Consumers: `src/Modules/Subjects/Holmes.Subjects.Application/EventHandlers/SubjectIntakeDataCapturedHandler.cs`

## Orders

- OrderCreatedFromIntakeIntegrationEvent
  - Contract: `src/Modules/Orders/Holmes.Orders.Contracts/IntegrationEvents/OrderCreatedFromIntakeIntegrationEvent.cs`
  - Publisher: `src/Modules/Orders/Holmes.Orders.Application/EventHandlers/OrderIntegrationEventPublisher.cs`
  - Consumers: `src/Modules/IntakeSessions/Holmes.IntakeSessions.Application/EventHandlers/OrderCreatedFromIntakeInviteHandler.cs`

- OrderStatusChangedIntegrationEvent
  - Contract: `src/Modules/Orders/Holmes.Orders.Contracts/IntegrationEvents/OrderStatusChangedIntegrationEvent.cs`
  - Publisher: `src/Modules/Orders/Holmes.Orders.Application/EventHandlers/OrderIntegrationEventPublisher.cs`
  - Consumers: `src/Modules/SlaClocks/Holmes.SlaClocks.Application/EventHandlers/OrderStatusChangedSlaHandler.cs`;
    `src/Modules/Services/Holmes.Services.Application/EventHandlers/OrderFulfillmentHandler.cs`

## Services

- ServiceCompletedIntegrationEvent
  - Contract: `src/Modules/Services/Holmes.Services.Contracts/IntegrationEvents/ServiceCompletedIntegrationEvent.cs`
  - Publisher: `src/Modules/Services/Holmes.Services.Application/EventHandlers/ServiceIntegrationEventPublisher.cs`
  - Consumers: `src/Modules/Orders/Holmes.Orders.Application/EventHandlers/ServiceCompletionOrderHandler.cs`

- ServicesDispatchedIntegrationEvent
  - Contract: `src/Modules/Services/Holmes.Services.Contracts/IntegrationEvents/ServicesDispatchedIntegrationEvent.cs`
  - Publisher: `src/Modules/Services/Holmes.Services.Application/EventHandlers/OrderFulfillmentHandler.cs`
  - Consumers: `src/Modules/Orders/Holmes.Orders.Application/EventHandlers/ServicesDispatchedOrderHandler.cs`

## Subjects

- SubjectIntakeRequestedIntegrationEvent
  - Contract: `src/Modules/Subjects/Holmes.Subjects.Contracts/IntegrationEvents/SubjectIntakeRequestedIntegrationEvent.cs`
  - Publisher: `src/Modules/Subjects/Holmes.Subjects.Application/EventHandlers/SubjectIntegrationEventPublisher.cs`
  - Consumers: `src/Modules/Orders/Holmes.Orders.Application/EventHandlers/SubjectIntakeRequestedOrderHandler.cs`

## SlaClocks

- SlaClockAtRiskIntegrationEvent
  - Contract: `src/Modules/SlaClocks/Holmes.SlaClocks.Contracts/IntegrationEvents/SlaClockAtRiskIntegrationEvent.cs`
  - Publisher: `src/Modules/SlaClocks/Holmes.SlaClocks.Application/EventHandlers/SlaClockIntegrationEventPublisher.cs`
  - Consumers: `src/Modules/Notifications/Holmes.Notifications.Application/EventHandlers/SlaClockAtRiskNotificationHandler.cs`

- SlaClockBreachedIntegrationEvent
  - Contract: `src/Modules/SlaClocks/Holmes.SlaClocks.Contracts/IntegrationEvents/SlaClockBreachedIntegrationEvent.cs`
  - Publisher: `src/Modules/SlaClocks/Holmes.SlaClocks.Application/EventHandlers/SlaClockIntegrationEventPublisher.cs`
  - Consumers: `src/Modules/Notifications/Holmes.Notifications.Application/EventHandlers/SlaClockBreachedNotificationHandler.cs`

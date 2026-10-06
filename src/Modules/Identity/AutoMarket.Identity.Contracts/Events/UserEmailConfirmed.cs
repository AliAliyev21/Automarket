using AutoMarket.BuildingBlocks.Messaging;

namespace AutoMarket.Identity.Contracts.Events;

[IntegrationEvent("identity.user.email-confirmed", 1)]
public sealed record UserEmailConfirmed(Guid UserId) : IIntegrationEvent;

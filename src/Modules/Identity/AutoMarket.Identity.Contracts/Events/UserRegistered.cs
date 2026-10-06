using AutoMarket.BuildingBlocks.Messaging;

namespace AutoMarket.Identity.Contracts.Events;

// ARCHITECTURE §5.4: MVP-də consumer yoxdur (gələcək analitika üçün)
[IntegrationEvent("identity.user.registered", 1)]
public sealed record UserRegistered(Guid UserId, DateTimeOffset RegisteredAt) : IIntegrationEvent;

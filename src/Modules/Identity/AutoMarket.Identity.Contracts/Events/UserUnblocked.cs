using AutoMarket.BuildingBlocks.Messaging;

namespace AutoMarket.Identity.Contracts.Events;

// FR-ADM-01 AC3 (ARCHITECTURE §5.4): Listings elanları yenidən göstərir
[IntegrationEvent("identity.user.unblocked", 1)]
public sealed record UserUnblocked(Guid UserId) : IIntegrationEvent;

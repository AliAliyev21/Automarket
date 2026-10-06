using AutoMarket.BuildingBlocks.Messaging;

namespace AutoMarket.Identity.Contracts.Events;

// FR-ADM-01 AC2 (ARCHITECTURE §5.4): Listings elanları axtarışdan gizlədir, Notifications bildirişləri dayandırır.
// Səbəb mətni event-də yoxdur
[IntegrationEvent("identity.user.blocked", 1)]
public sealed record UserBlocked(Guid UserId, DateTimeOffset BlockedAt) : IIntegrationEvent;

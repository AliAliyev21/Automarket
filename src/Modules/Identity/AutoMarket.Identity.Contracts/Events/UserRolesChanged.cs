using AutoMarket.BuildingBlocks.Messaging;

namespace AutoMarket.Identity.Contracts.Events;

// FR-ADM-02 (ARCHITECTURE §5.4): dəyişiklikdən sonrakı tam rol siyahısı. MVP-də consumer yoxdur
[IntegrationEvent("identity.user.roles-changed", 1)]
public sealed record UserRolesChanged(Guid UserId, IReadOnlyList<string> Roles) : IIntegrationEvent;

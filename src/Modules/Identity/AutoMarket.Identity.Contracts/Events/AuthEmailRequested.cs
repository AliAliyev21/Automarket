using AutoMarket.BuildingBlocks.Messaging;
using AutoMarket.BuildingBlocks.Security;

namespace AutoMarket.Identity.Contracts.Events;

// ARCHITECTURE §5.4: email ehtiva edən yeganə event (istisna). Link olan məktublarda xam token Data Protection ilə
// şifrələnib (ProtectorPurpose), outbox-da və broker-də açıq saxlanılmır. Payload log-a yazılmır (SEC-LOG-01)
[IntegrationEvent("identity.auth-email.requested", 1)]
[LogRedact]
public sealed record AuthEmailRequested(Guid UserId, string Email, AuthEmailKind Kind, string? ProtectedToken) : IIntegrationEvent
{
    public const string ProtectorPurpose = "AutoMarket.AuthEmail.v1";
}

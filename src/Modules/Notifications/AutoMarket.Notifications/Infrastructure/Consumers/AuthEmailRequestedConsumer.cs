using AutoMarket.BuildingBlocks.Messaging;
using AutoMarket.Identity.Contracts.Events;
using AutoMarket.Notifications.Application.Abstractions;
using AutoMarket.Notifications.Application.AuthEmails;
using Microsoft.AspNetCore.DataProtection;

namespace AutoMarket.Notifications.Infrastructure.Consumers;

// ARCHITECTURE §6.1: token açılır, az+en şablon render olunur, SMTP-yə göndərilir. Göndərmə uğursuz olarsa exception
// consumer host-un retry/DLQ mexanizminə düşür. Məktub məzmunu və token log-a yazılmır (SEC-LOG-01)
internal sealed class AuthEmailRequestedConsumer(
    IDataProtectionProvider dataProtection,
    AuthEmailRenderer renderer,
    IEmailTransport transport) : IIntegrationEventConsumer<AuthEmailRequested>
{
    public async Task ConsumeAsync(AuthEmailRequested integrationEvent, MessageContext context, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(integrationEvent);

        var token = integrationEvent.ProtectedToken is null
            ? null
            : dataProtection
                .CreateProtector(AuthEmailRequested.ProtectorPurpose)
                .ToTimeLimitedDataProtector()
                .Unprotect(integrationEvent.ProtectedToken);

        var message = renderer.Render(integrationEvent.Email, integrationEvent.Kind, token);
        await transport.SendAsync(message, cancellationToken);
    }
}

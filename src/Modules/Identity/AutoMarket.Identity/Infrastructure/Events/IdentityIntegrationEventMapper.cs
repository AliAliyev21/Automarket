using AutoMarket.BuildingBlocks.Domain;
using AutoMarket.BuildingBlocks.Messaging;
using AutoMarket.Identity.Contracts.Events;
using AutoMarket.Identity.Domain.Users.Events;
using AutoMarket.Identity.Infrastructure.Persistence;
using Microsoft.AspNetCore.DataProtection;

namespace AutoMarket.Identity.Infrastructure.Events;

// Domen hadisəsi → integration event (CONVENTIONS §5.6). ARCHITECTURE §5.4: link olan məktubun xam tokeni Data Protection
// ilə şifrələnir (vaxtı tokenin özü qədər məhduddur) ki, outbox-da və broker-də açıq saxlanılmasın
internal sealed class IdentityIntegrationEventMapper(IDataProtectionProvider dataProtection) : IIntegrationEventMapper<IdentityDbContext>
{
    public IEnumerable<IIntegrationEvent> Map(IDomainEvent domainEvent) => domainEvent switch
    {
        UserRegisteredDomainEvent registered =>
            [new UserRegistered(registered.UserId, registered.RegisteredAt)],

        UserEmailConfirmedDomainEvent confirmed =>
            [new UserEmailConfirmed(confirmed.UserId)],

        EmailConfirmationRequestedDomainEvent requested =>
            [new AuthEmailRequested(requested.UserId, requested.Email, AuthEmailKind.ConfirmEmail, Protect(requested.RawToken, requested.ExpiresAt))],

        RegistrationAttemptedOnExistingAccountDomainEvent attempted =>
            [new AuthEmailRequested(attempted.UserId, attempted.Email, AuthEmailKind.RegistrationAttemptOnExistingAccount, null)],

        // SEC-AUTH-03: lockout istifadəçiyə email ilə bildirilir
        UserLockedOutDomainEvent lockedOut =>
            [new AuthEmailRequested(lockedOut.UserId, lockedOut.Email, AuthEmailKind.LockedOut, null)],

        _ => [],
    };

    private string Protect(string rawToken, DateTimeOffset expiresAt) =>
        dataProtection
            .CreateProtector(AuthEmailRequested.ProtectorPurpose)
            .ToTimeLimitedDataProtector()
            .Protect(rawToken, expiresAt);
}

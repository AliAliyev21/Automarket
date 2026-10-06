using AutoMarket.BuildingBlocks.Domain;

namespace AutoMarket.Identity.Domain.Users.Events;

internal sealed record RegistrationAttemptedOnExistingAccountDomainEvent(Guid UserId, string Email) : IDomainEvent;

using AutoMarket.BuildingBlocks.Domain;

namespace AutoMarket.Identity.Domain.Users.Events;

internal sealed record PasswordChangedDomainEvent(Guid UserId, string Email) : IDomainEvent;

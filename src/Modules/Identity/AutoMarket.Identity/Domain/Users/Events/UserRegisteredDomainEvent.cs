using AutoMarket.BuildingBlocks.Domain;

namespace AutoMarket.Identity.Domain.Users.Events;

internal sealed record UserRegisteredDomainEvent(Guid UserId, DateTimeOffset RegisteredAt) : IDomainEvent;

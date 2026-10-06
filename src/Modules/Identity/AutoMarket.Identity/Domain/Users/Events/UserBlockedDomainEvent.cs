using AutoMarket.BuildingBlocks.Domain;

namespace AutoMarket.Identity.Domain.Users.Events;

internal sealed record UserBlockedDomainEvent(Guid UserId, DateTimeOffset BlockedAt) : IDomainEvent;

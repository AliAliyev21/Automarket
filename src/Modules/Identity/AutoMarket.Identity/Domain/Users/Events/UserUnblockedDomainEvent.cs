using AutoMarket.BuildingBlocks.Domain;

namespace AutoMarket.Identity.Domain.Users.Events;

internal sealed record UserUnblockedDomainEvent(Guid UserId) : IDomainEvent;

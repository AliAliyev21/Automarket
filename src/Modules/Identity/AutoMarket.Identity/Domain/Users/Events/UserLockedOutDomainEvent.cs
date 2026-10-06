using AutoMarket.BuildingBlocks.Domain;

namespace AutoMarket.Identity.Domain.Users.Events;

internal sealed record UserLockedOutDomainEvent(Guid UserId, string Email, DateTimeOffset LockoutEnd) : IDomainEvent;

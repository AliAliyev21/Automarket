using AutoMarket.BuildingBlocks.Domain;

namespace AutoMarket.Identity.Domain.Users.Events;

internal sealed record UserRolesChangedDomainEvent(Guid UserId, IReadOnlyList<string> Roles) : IDomainEvent;

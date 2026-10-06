using AutoMarket.BuildingBlocks.Domain;

namespace AutoMarket.Identity.Domain.Users.Events;

// RawToken heç yerdə saxlanılmır və log-a yazılmır (SEC-LOG-01)
internal sealed record PasswordResetRequestedDomainEvent(Guid UserId, string Email, string RawToken, DateTimeOffset ExpiresAt) : IDomainEvent;

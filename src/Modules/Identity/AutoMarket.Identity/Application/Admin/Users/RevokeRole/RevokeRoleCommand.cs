namespace AutoMarket.Identity.Application.Admin.Users.RevokeRole;

internal sealed record RevokeRoleCommand(Guid ActorId, Guid UserId, string Role);

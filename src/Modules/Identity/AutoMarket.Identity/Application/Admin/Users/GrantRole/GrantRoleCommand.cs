namespace AutoMarket.Identity.Application.Admin.Users.GrantRole;

internal sealed record GrantRoleCommand(Guid ActorId, Guid UserId, string Role);

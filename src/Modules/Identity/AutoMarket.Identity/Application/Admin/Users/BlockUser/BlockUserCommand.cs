namespace AutoMarket.Identity.Application.Admin.Users.BlockUser;

internal sealed record BlockUserCommand(Guid ActorId, Guid UserId, string Reason);

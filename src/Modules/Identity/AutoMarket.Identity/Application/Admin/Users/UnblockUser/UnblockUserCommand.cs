namespace AutoMarket.Identity.Application.Admin.Users.UnblockUser;

internal sealed record UnblockUserCommand(Guid ActorId, Guid UserId);

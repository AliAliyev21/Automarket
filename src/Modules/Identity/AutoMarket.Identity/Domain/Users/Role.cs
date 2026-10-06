using Microsoft.AspNetCore.Identity;

namespace AutoMarket.Identity.Domain.Users;

// Rollar sabitdir (User, Moderator, Admin) və migration ilə seed olunur
internal sealed class Role : IdentityRole<Guid>;

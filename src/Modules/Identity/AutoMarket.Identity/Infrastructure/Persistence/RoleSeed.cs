using AutoMarket.BuildingBlocks.Application;
using AutoMarket.Identity.Domain.Users;

namespace AutoMarket.Identity.Infrastructure.Persistence;

// REQUIREMENTS 2.1: rollar sabitdir və migration ilə yaradılır. Id-lər dəyişdirilmir (migration-da istifadə olunur)
internal static class RoleSeed
{
    public static readonly Guid UserRoleId = Guid.Parse("0199b8a0-0000-7000-8000-000000000001");
    public static readonly Guid ModeratorRoleId = Guid.Parse("0199b8a0-0000-7000-8000-000000000002");
    public static readonly Guid AdminRoleId = Guid.Parse("0199b8a0-0000-7000-8000-000000000003");

    public static Role[] All =>
    [
        Create(UserRoleId, Roles.User),
        Create(ModeratorRoleId, Roles.Moderator),
        Create(AdminRoleId, Roles.Admin),
    ];

    private static Role Create(Guid id, string name) => new()
    {
        Id = id,
        Name = name,
        NormalizedName = name.ToUpperInvariant(),
        ConcurrencyStamp = id.ToString(),
    };
}

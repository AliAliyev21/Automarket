using AutoMarket.Identity.Application.Abstractions;
using AutoMarket.Identity.Application.Me.GetMe;
using Microsoft.EntityFrameworkCore;

namespace AutoMarket.Identity.Infrastructure.Persistence.Queries;

// Oxuma: AsNoTracking + projection, yalnız lazımi sütunlar (CONVENTIONS §7.3)
internal sealed class UserQueries(IdentityDbContext db) : IUserQueries
{
    public async Task<MeResponse?> GetMeAsync(Guid userId, CancellationToken cancellationToken)
    {
        var user = await db.Users
            .AsNoTracking()
            .Where(u => u.Id == userId)
            .Select(u => new { u.Id, u.Email, u.Name, u.PhoneNumber, u.CreatedAt })
            .FirstOrDefaultAsync(cancellationToken);

        if (user is null)
        {
            return null;
        }

        var roles = await (from userRole in db.UserRoles
                           join role in db.Roles on userRole.RoleId equals role.Id
                           where userRole.UserId == userId
                           orderby role.Name
                           select role.Name!)
            .ToListAsync(cancellationToken);

        return new MeResponse(user.Id, user.Email!, user.Name, user.PhoneNumber, roles, user.CreatedAt);
    }
}

using AutoMarket.Identity.Application.Abstractions;
using AutoMarket.Identity.Domain.Users;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace AutoMarket.Identity.Infrastructure.Persistence.Repositories;

internal sealed class UserRepository(IdentityDbContext db, UserManager<User> userManager) : IUserRepository
{
    public Task<User?> FindByEmailAsync(string normalizedEmail, CancellationToken cancellationToken)
    {
        // Identity-nin öz normallaşdırması (upper invariant) unikal indeksli sütunla eynidir
        var lookupKey = userManager.NormalizeEmail(normalizedEmail);
        return db.Users.FirstOrDefaultAsync(user => user.NormalizedEmail == lookupKey, cancellationToken);
    }

    public Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        db.Users.FirstOrDefaultAsync(user => user.Id == id, cancellationToken);

    // UserStore.AutoSaveChanges = false: istifadəçi yalnız kontekstə əlavə olunur, SaveChanges handler-dədir
    public async Task CreateAsync(User user, string password, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(user);

        var result = await userManager.CreateAsync(user, password);
        if (!result.Succeeded)
        {
            // Validasiya (format, siyasət) handler-dən əvvəl keçib; burada uğursuzluq proqram xətasıdır
            throw new InvalidOperationException(
                $"User creation failed: {string.Join(", ", result.Errors.Select(error => error.Code))}");
        }

        // FR-AUTH-01 AC6: yeni istifadəçiyə yalnız User rolu
        db.UserRoles.Add(new IdentityUserRole<Guid> { UserId = user.Id, RoleId = RoleSeed.UserRoleId });
    }

    public async Task<IReadOnlyList<string>> GetRolesAsync(Guid userId, CancellationToken cancellationToken) =>
        await (from userRole in db.UserRoles
               join role in db.Roles on userRole.RoleId equals role.Id
               where userRole.UserId == userId
               orderby role.Name
               select role.Name!)
            .ToListAsync(cancellationToken);

    public async Task<bool> AddRoleAsync(Guid userId, string role, CancellationToken cancellationToken)
    {
        var roleId = RoleSeed.IdOf(role);
        if (await db.UserRoles.AnyAsync(userRole => userRole.UserId == userId && userRole.RoleId == roleId, cancellationToken))
        {
            return false;
        }

        db.UserRoles.Add(new IdentityUserRole<Guid> { UserId = userId, RoleId = roleId });
        return true;
    }

    public async Task<bool> RemoveRoleAsync(Guid userId, string role, CancellationToken cancellationToken)
    {
        var roleId = RoleSeed.IdOf(role);
        var userRole = await db.UserRoles.FirstOrDefaultAsync(
            candidate => candidate.UserId == userId && candidate.RoleId == roleId,
            cancellationToken);
        if (userRole is null)
        {
            return false;
        }

        db.UserRoles.Remove(userRole);
        return true;
    }

    public Task<bool> AnyActiveAdminAsync(CancellationToken cancellationToken) =>
        (from userRole in db.UserRoles
         join user in db.Users on userRole.UserId equals user.Id
         where userRole.RoleId == RoleSeed.AdminRoleId && user.Status == UserStatus.Active
         select user.Id)
            .AnyAsync(cancellationToken);
}

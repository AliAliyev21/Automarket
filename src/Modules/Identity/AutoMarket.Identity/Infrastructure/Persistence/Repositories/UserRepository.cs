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
}

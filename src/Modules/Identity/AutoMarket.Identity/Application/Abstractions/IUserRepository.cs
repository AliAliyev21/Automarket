using AutoMarket.Identity.Domain.Users;

namespace AutoMarket.Identity.Application.Abstractions;

internal interface IUserRepository
{
    public Task<User?> FindByEmailAsync(string normalizedEmail, CancellationToken cancellationToken);

    public Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    // UserManager ilə: şifrə hash-i, normallaşdırma, security stamp; rol yalnız User (FR-AUTH-01 AC6). SaveChanges çağırılmır
    public Task CreateAsync(User user, string password, CancellationToken cancellationToken);

    public Task<IReadOnlyList<string>> GetRolesAsync(Guid userId, CancellationToken cancellationToken);
}

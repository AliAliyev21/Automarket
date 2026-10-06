using AutoMarket.BuildingBlocks.Application;
using AutoMarket.BuildingBlocks.Persistence;
using AutoMarket.Identity.Application.Abstractions;
using AutoMarket.Identity.Domain.Tokens;
using AutoMarket.Identity.Domain.Users;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace AutoMarket.Identity.Infrastructure.Persistence;

// ARCHITECTURE §4.2: Identity-nin EF store-u identity schema-sına map olunur, cədvəl adları dəyişdirilir (AspNetUsers → users).
// İstifadə olunmayan Identity cədvəlləri (claims, logins, tokens, role claims) map olunmur
internal sealed class IdentityDbContext(DbContextOptions<IdentityDbContext> options)
    : Microsoft.AspNetCore.Identity.EntityFrameworkCore.IdentityDbContext<User, Role, Guid>(options), IIdentityUnitOfWork
{
    public const string SchemaName = "identity";

    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    public DbSet<OneTimeToken> OneTimeTokens => Set<OneTimeToken>();

    public async Task<IUnitOfWorkTransaction> BeginTransactionAsync(CancellationToken cancellationToken) =>
        new EfUnitOfWorkTransaction(await Database.BeginTransactionAsync(cancellationToken));

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.HasDefaultSchema(SchemaName);
        builder.ConfigureMessagingTables();

        builder.Ignore<IdentityUserClaim<Guid>>();
        builder.Ignore<IdentityUserLogin<Guid>>();
        builder.Ignore<IdentityUserToken<Guid>>();
        builder.Ignore<IdentityRoleClaim<Guid>>();

        builder.ApplyConfigurationsFromAssembly(
            typeof(IdentityDbContext).Assembly,
            type => string.Equals(type.Namespace, typeof(IdentityDbContext).Namespace + ".Configurations", StringComparison.Ordinal));
    }
}

internal sealed class IdentityDbContextFactory : IDesignTimeDbContextFactory<IdentityDbContext>
{
    public IdentityDbContext CreateDbContext(string[] args) =>
        new(ModuleDatabaseOptions.ForDesignTime<IdentityDbContext>(IdentityDbContext.SchemaName));
}

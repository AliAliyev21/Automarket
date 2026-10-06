using AutoMarket.BuildingBlocks.Persistence;
using Microsoft.AspNetCore.DataProtection.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace AutoMarket.BuildingBlocks.Platform;

// Schema platform: Data Protection açarları (bütün instansiyalar üçün ortaq açar halqası, ARCHITECTURE §7.2 SEC-SEC-05).
// job_runs background job-lar mərhələsində əlavə olunacaq
internal sealed class PlatformDbContext(DbContextOptions<PlatformDbContext> options) : ModuleDbContext(options), IDataProtectionKeyContext
{
    public const string SchemaName = "platform";

    public DbSet<DataProtectionKey> DataProtectionKeys => Set<DataProtectionKey>();

    protected override string Schema => SchemaName;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<DataProtectionKey>(builder =>
        {
            builder.ToTable("data_protection_keys");
            builder.Property(x => x.FriendlyName).HasMaxLength(200);
        });
    }
}

internal sealed class PlatformDbContextFactory : IDesignTimeDbContextFactory<PlatformDbContext>
{
    public PlatformDbContext CreateDbContext(string[] args) =>
        new(ModuleDatabaseOptions.ForDesignTime<PlatformDbContext>(PlatformDbContext.SchemaName));
}

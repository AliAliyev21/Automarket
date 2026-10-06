using AutoMarket.BuildingBlocks.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace AutoMarket.BuildingBlocks.Audit;

// Schema audit: audit_log, outbox, inbox (ARCHITECTURE §3.1)
internal sealed class AuditDbContext(DbContextOptions<AuditDbContext> options) : ModuleDbContext(options)
{
    public const string SchemaName = "audit";

    public DbSet<AuditLogEntry> AuditLog => Set<AuditLogEntry>();

    protected override string Schema => SchemaName;
}

internal sealed class AuditDbContextFactory : IDesignTimeDbContextFactory<AuditDbContext>
{
    public AuditDbContext CreateDbContext(string[] args) =>
        new(ModuleDatabaseOptions.ForDesignTime<AuditDbContext>(AuditDbContext.SchemaName));
}

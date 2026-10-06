using Microsoft.EntityFrameworkCore;

namespace AutoMarket.BuildingBlocks.Persistence;

// Modul DbContext-lərinin bazası: öz schema-sı, outbox və inbox (ARCHITECTURE §4.2). Konfiqurasiyalar yalnız
// öz namespace-indən tətbiq olunur ki, eyni assembly-dəki başqa DbContext-in entity-ləri qarışmasın (CONVENTIONS §7.1)
public abstract class ModuleDbContext(DbContextOptions options) : DbContext(options)
{
    protected abstract string Schema { get; }

    // Default: <DbContext namespace>.Configurations
    protected virtual string ConfigurationNamespace => $"{GetType().Namespace}.Configurations";

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);

        modelBuilder.HasDefaultSchema(Schema);
        modelBuilder.ConfigureMessagingTables();
        modelBuilder.ApplyConfigurationsFromAssembly(
            GetType().Assembly,
            type => string.Equals(type.Namespace, ConfigurationNamespace, StringComparison.Ordinal));
    }
}

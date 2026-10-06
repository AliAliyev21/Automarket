using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace AutoMarket.BuildingBlocks.Persistence;

// Development-də startup-da və integration test fixture-unda bütün modulların migration-ları tətbiq olunur (ARCHITECTURE §4.4)
public interface IModuleMigrator
{
    public string Name { get; }

    public Task MigrateAsync(CancellationToken cancellationToken);
}

internal sealed class ModuleMigrator<TContext>(IServiceScopeFactory scopeFactory) : IModuleMigrator
    where TContext : DbContext
{
    public string Name => typeof(TContext).Name;

    public async Task MigrateAsync(CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<TContext>();
        await context.Database.MigrateAsync(cancellationToken);
    }
}

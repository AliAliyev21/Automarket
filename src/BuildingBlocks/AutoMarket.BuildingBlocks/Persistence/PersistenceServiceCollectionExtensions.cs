using AutoMarket.BuildingBlocks.Application;
using AutoMarket.BuildingBlocks.Audit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Npgsql;

namespace AutoMarket.BuildingBlocks.Persistence;

public static class PersistenceServiceCollectionExtensions
{
    // Bütün DbContext-lər Host-un paylaşılan NpgsqlDataSource-unu istifadə edir (ARCHITECTURE §4.2)
    public static IServiceCollection AddModuleDbContext<TContext>(this IServiceCollection services, string schema)
        where TContext : DbContext
    {
        services.TryAddSingleton<IRequestContext, NoRequestContext>();
        services.TryAddScoped<AuditLog>();
        services.TryAddScoped<IAuditLog>(serviceProvider => serviceProvider.GetRequiredService<AuditLog>());

        services.AddScoped<OutboxInterceptor<TContext>>();
        services.AddDbContext<TContext>((serviceProvider, options) => options
            .UseModuleDatabase(serviceProvider.GetRequiredService<NpgsqlDataSource>(), schema)
            .AddInterceptors(serviceProvider.GetRequiredService<OutboxInterceptor<TContext>>()));
        services.AddSingleton<IModuleMigrator, ModuleMigrator<TContext>>();

        return services;
    }
}

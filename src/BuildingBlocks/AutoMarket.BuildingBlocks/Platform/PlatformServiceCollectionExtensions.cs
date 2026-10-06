using AutoMarket.BuildingBlocks.Persistence;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.DependencyInjection;

namespace AutoMarket.BuildingBlocks.Platform;

public static class PlatformServiceCollectionExtensions
{
    public const string DataProtectionApplicationName = "AutoMarket";

    // Data Protection açarları platform.data_protection_keys-də saxlanılır və avtomatik rotasiya olunur (ARCHITECTURE §7.2)
    public static IServiceCollection AddPlatform(this IServiceCollection services)
    {
        services.AddModuleDbContext<PlatformDbContext>(PlatformDbContext.SchemaName);

        services.AddDataProtection()
            .SetApplicationName(DataProtectionApplicationName)
            .SetDefaultKeyLifetime(TimeSpan.FromDays(90))
            .PersistKeysToDbContext<PlatformDbContext>();

        return services;
    }
}

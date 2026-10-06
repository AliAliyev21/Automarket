using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace AutoMarket.BuildingBlocks.Security;

public static class SecurityServiceCollectionExtensions
{
    public static IServiceCollection AddBuildingBlocksSecurity(this IServiceCollection services)
    {
        services.TryAddSingleton<ISecureTokenGenerator, SecureTokenGenerator>();

        return services;
    }
}

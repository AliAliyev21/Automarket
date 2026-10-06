using AutoMarket.BuildingBlocks.Application;
using AutoMarket.BuildingBlocks.Web.Security;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace AutoMarket.BuildingBlocks.Web;

public static class WebServiceCollectionExtensions
{
    public static IServiceCollection AddBuildingBlocksWeb(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddHttpContextAccessor();
        services.TryAddScoped<ICurrentUser, HttpCurrentUser>();

        // Audit qeydində IP və user agent (SEC-LOG-04): BuildingBlocks-un HTTP-siz default-unu əvəz edir
        services.Replace(ServiceDescriptor.Singleton<IRequestContext, HttpRequestContext>());

        services.AddOptions<AllowedOriginsOptions>()
            .Bind(configuration.GetSection(AllowedOriginsOptions.SectionName))
            .ValidateOnStart();
        services.AddSingleton<IValidateOptions<AllowedOriginsOptions>, AllowedOriginsOptionsValidator>();

        return services;
    }
}

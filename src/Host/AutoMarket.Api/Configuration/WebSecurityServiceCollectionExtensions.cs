using System.Net;
using AutoMarket.BuildingBlocks.Web.Security;
using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Options;

namespace AutoMarket.Api.Configuration;

internal static class WebSecurityServiceCollectionExtensions
{
    public const string ForwardedHeadersSection = "ForwardedHeaders";

    // SEC-NET-03: yalnız konfiqurasiyadakı origin-lər, wildcard yoxdur; credentials refresh cookie üçün lazımdır (ARCHITECTURE §7.2)
    public static IServiceCollection AddAutoMarketCors(this IServiceCollection services)
    {
        services.AddCors();
        services.AddOptions<CorsOptions>()
            .Configure<IOptions<AllowedOriginsOptions>>((cors, allowed) => cors.AddDefaultPolicy(policy => policy
                .WithOrigins([.. allowed.Value.AllowedOrigins])
                .AllowCredentials()
                .WithMethods("GET", "POST", "PUT", "PATCH", "DELETE")
                .WithHeaders("Authorization", "Content-Type", "X-Correlation-Id", "If-Match")));

        return services;
    }

    // SEC-RATE-12: X-Forwarded-For yalnız konfiqurasiyada göstərilmiş etibarlı proxy-lərdən qəbul olunur.
    // Siyahı boşdursa heç bir header-ə etibar edilmir (default loopback istisnası da silinir)
    public static IServiceCollection AddTrustedForwardedHeaders(this IServiceCollection services, IConfiguration configuration)
    {
        var knownProxies = configuration.GetSection($"{ForwardedHeadersSection}:KnownProxies").Get<string[]>() ?? [];

        services.Configure<ForwardedHeadersOptions>(options =>
        {
            options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
            options.KnownIPNetworks.Clear();
            options.KnownProxies.Clear();

            foreach (var proxy in knownProxies)
            {
                options.KnownProxies.Add(IPAddress.Parse(proxy));
            }
        });

        return services;
    }
}

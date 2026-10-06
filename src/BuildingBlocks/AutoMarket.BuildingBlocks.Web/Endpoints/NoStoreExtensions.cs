using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace AutoMarket.BuildingBlocks.Web.Endpoints;

// SEC-NET-02: autentifikasiya və şəxsi məlumat qaytaran cavablarda Cache-Control: no-store (xəta cavabları da daxil)
public static class NoStoreExtensions
{
    public static TBuilder NoStore<TBuilder>(this TBuilder builder)
        where TBuilder : IEndpointConventionBuilder
    {
        builder.AddEndpointFilter(static async (context, next) =>
        {
            var response = context.HttpContext.Response;
            response.OnStarting(() =>
            {
                response.Headers.CacheControl = "no-store";
                response.Headers.Pragma = "no-cache";
                return Task.CompletedTask;
            });

            return await next(context);
        });

        return builder;
    }
}

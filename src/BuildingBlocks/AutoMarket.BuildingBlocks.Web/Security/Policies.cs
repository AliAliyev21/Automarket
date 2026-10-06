using AutoMarket.BuildingBlocks.Application;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace AutoMarket.BuildingBlocks.Web.Security;

// SEC-AUTHZ-01: policy adları sabitlərdən gəlir, string literal yazılmır (CONVENTIONS §6.8)
public static class Policies
{
    public const string User = "User";
    public const string Moderator = "Moderator";
    public const string Admin = "Admin";

    // FallbackPolicy: policy-si olmayan endpoint anonim deyil. Endpoint-i olmayan (naməlum) route-a tətbiq olunmur ki,
    // o, 401 əvəzinə 404 qaytarsın. Rol iyerarxiyası: Moderator policy = Moderator və ya Admin (ARCHITECTURE §7.2)
    public static IServiceCollection AddAutoMarketAuthorization(this IServiceCollection services)
    {
        services.AddAuthorizationBuilder()
            .SetFallbackPolicy(new AuthorizationPolicyBuilder()
                .RequireAssertion(context =>
                    context.User.Identity?.IsAuthenticated == true
                    || (context.Resource is HttpContext httpContext && httpContext.GetEndpoint() is null))
                .Build())
            .AddPolicy(User, policy => policy.RequireAuthenticatedUser().RequireRole(Roles.User, Roles.Moderator, Roles.Admin))
            .AddPolicy(Moderator, policy => policy.RequireAuthenticatedUser().RequireRole(Roles.Moderator, Roles.Admin))
            .AddPolicy(Admin, policy => policy.RequireAuthenticatedUser().RequireRole(Roles.Admin));

        return services;
    }
}

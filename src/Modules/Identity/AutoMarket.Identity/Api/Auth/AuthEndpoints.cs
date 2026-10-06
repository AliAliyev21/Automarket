using AutoMarket.BuildingBlocks.Web.Endpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace AutoMarket.Identity.Api.Auth;

// /api/v1/auth: bütün cavablar Cache-Control: no-store (SEC-NET-02). Hər endpoint öz authorization-unu açıq yazır
internal static class AuthEndpoints
{
    public const string RoutePrefix = "/auth";

    public static RouteGroupBuilder MapAuthEndpoints(this RouteGroupBuilder api)
    {
        var group = api.MapGroup(RoutePrefix).WithTags("Auth").NoStore();

        group.MapRegister();
        group.MapConfirmEmail();
        group.MapResendConfirmation();
        group.MapLogin();
        group.MapRefreshSession();
        group.MapLogout();
        group.MapLogoutAll();
        group.MapForgotPassword();
        group.MapResetPassword();
        group.MapChangePassword();

        return group;
    }
}

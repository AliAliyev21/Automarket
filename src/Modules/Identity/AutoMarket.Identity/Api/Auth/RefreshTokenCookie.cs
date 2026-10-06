using AutoMarket.BuildingBlocks.Web.Endpoints;
using Microsoft.AspNetCore.Http;

namespace AutoMarket.Identity.Api.Auth;

// SEC-NET-04: refresh token yalnız cookie-də. HttpOnly, Secure, SameSite=Strict, Path yalnız auth endpoint-ləri,
// Domain göstərilmir, Max-Age refresh tokenin ömrünə bərabərdir
internal static class RefreshTokenCookie
{
    public const string Name = "rt";
    public const string Path = ApiRoutes.V1Prefix + AuthEndpoints.RoutePrefix;

    public static string? Read(HttpRequest request) => request.Cookies[Name];

    public static void Append(HttpResponse response, string refreshToken, DateTimeOffset expiresAt, DateTimeOffset now)
    {
        var options = CreateOptions();
        options.MaxAge = expiresAt - now;
        response.Cookies.Append(Name, refreshToken, options);
    }

    // Reuse aşkarlananda və token etibarsız olanda cookie silinir (boş dəyər + keçmiş tarix)
    public static void Delete(HttpResponse response) => response.Cookies.Delete(Name, CreateOptions());

    private static CookieOptions CreateOptions() => new()
    {
        HttpOnly = true,
        Secure = true,
        SameSite = SameSiteMode.Strict,
        Path = Path,
        IsEssential = true,
    };
}

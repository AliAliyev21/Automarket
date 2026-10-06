using AutoMarket.BuildingBlocks.Application;
using AutoMarket.BuildingBlocks.Web.Errors;
using AutoMarket.BuildingBlocks.Web.Security;
using AutoMarket.Identity.Application.Auth;
using AutoMarket.Identity.Application.Auth.RefreshSession;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;

namespace AutoMarket.Identity.Api.Auth;

// FR-AUTH-04. Access token tələb olunmur (vaxtı keçmiş ola bilər), kimlik refresh cookie-dən gəlir (icazə matrisi sətir 2).
// SEC-NET-04: cookie qəbul etdiyi üçün Origin yoxlanılır; body rejimi (native client) MVP-də yoxdur
internal static class RefreshSessionEndpoint
{
    public static RouteGroupBuilder MapRefreshSession(this RouteGroupBuilder group)
    {
        group.MapPost("/refresh", HandleAsync)
            .WithName("RefreshSession")
            .AllowAnonymous()
            .AddEndpointFilter<OriginCheckFilter>()
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status429TooManyRequests);

        return group;
    }

    private static async Task<Results<Ok<AccessTokenResponse>, ApiProblemResult>> HandleAsync(
        HttpRequest request,
        HttpResponse response,
        ICommandHandler<RefreshSessionCommand, SessionTokens> handler,
        TimeProvider time,
        CancellationToken cancellationToken)
    {
        var refreshToken = RefreshTokenCookie.Read(request);
        if (string.IsNullOrEmpty(refreshToken))
        {
            return CommonErrors.Unauthorized.ToProblem();
        }

        var result = await handler.HandleAsync(new RefreshSessionCommand(refreshToken), cancellationToken);
        if (result.IsFailure)
        {
            // Limit aşılıbsa token hələ etibarlıdır, cookie saxlanılır
            if (result.Error!.Code != ErrorCodes.RateLimited)
            {
                RefreshTokenCookie.Delete(response);
            }

            return result.ToProblem();
        }

        RefreshTokenCookie.Append(response, result.Value.RefreshToken, result.Value.RefreshTokenExpiresAt, time.GetUtcNow());
        return TypedResults.Ok(AccessTokenResponse.From(result.Value));
    }
}

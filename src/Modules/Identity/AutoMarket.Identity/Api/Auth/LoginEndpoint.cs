using AutoMarket.BuildingBlocks.Application;
using AutoMarket.BuildingBlocks.Web.Errors;
using AutoMarket.BuildingBlocks.Web.RateLimiting;
using AutoMarket.BuildingBlocks.Web.Validation;
using AutoMarket.Identity.Application.Auth;
using AutoMarket.Identity.Application.Auth.Login;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;

namespace AutoMarket.Identity.Api.Auth;

// FR-AUTH-03 AC1: access token body-də, refresh token yalnız HttpOnly cookie-də (SEC-NET-04)
internal static class LoginEndpoint
{
    public static RouteGroupBuilder MapLogin(this RouteGroupBuilder group)
    {
        group.MapPost("/login", HandleAsync)
            .WithName("Login")
            .AllowAnonymous()
            .RequireRateLimiting(RateLimitPolicies.Login)
            .AddEndpointFilter<ValidationFilter<LoginRequest>>()
            .Accepts<LoginRequest>("application/json")
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status429TooManyRequests);

        return group;
    }

    private static async Task<Results<Ok<AccessTokenResponse>, ApiProblemResult>> HandleAsync(
        LoginRequest request,
        HttpResponse response,
        ICommandHandler<LoginCommand, SessionTokens> handler,
        TimeProvider time,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new LoginCommand(request.Email!, request.Password!), cancellationToken);
        if (result.IsFailure)
        {
            return result.ToProblem();
        }

        RefreshTokenCookie.Append(response, result.Value.RefreshToken, result.Value.RefreshTokenExpiresAt, time.GetUtcNow());
        return TypedResults.Ok(AccessTokenResponse.From(result.Value));
    }
}

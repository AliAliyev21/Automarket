using AutoMarket.BuildingBlocks.Application;
using AutoMarket.BuildingBlocks.Web.Errors;
using AutoMarket.BuildingBlocks.Web.RateLimiting;
using AutoMarket.BuildingBlocks.Web.Validation;
using AutoMarket.Identity.Application.Auth.ForgotPassword;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;

namespace AutoMarket.Identity.Api.Auth;

// FR-AUTH-06 AC1, SEC-AUTH-08: həmişə 202. SEC-RATE-03: IP limiti middleware-də, email limiti handler-də
internal static class ForgotPasswordEndpoint
{
    public static RouteGroupBuilder MapForgotPassword(this RouteGroupBuilder group)
    {
        group.MapPost("/forgot-password", HandleAsync)
            .WithName("ForgotPassword")
            .AllowAnonymous()
            .RequireRateLimiting(RateLimitPolicies.Recovery)
            .AddEndpointFilter<ValidationFilter<ForgotPasswordRequest>>()
            .Accepts<ForgotPasswordRequest>("application/json")
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status429TooManyRequests);

        return group;
    }

    private static async Task<Results<Accepted, ApiProblemResult>> HandleAsync(
        ForgotPasswordRequest request,
        ICommandHandler<ForgotPasswordCommand> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new ForgotPasswordCommand(request.Email!), cancellationToken);
        return result.IsSuccess ? TypedResults.Accepted((string?)null) : result.ToProblem();
    }
}

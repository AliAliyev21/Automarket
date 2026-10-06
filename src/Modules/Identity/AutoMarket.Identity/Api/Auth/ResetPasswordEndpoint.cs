using AutoMarket.BuildingBlocks.Application;
using AutoMarket.BuildingBlocks.Web.Errors;
using AutoMarket.BuildingBlocks.Web.RateLimiting;
using AutoMarket.BuildingBlocks.Web.Validation;
using AutoMarket.Identity.Application.Auth.ResetPassword;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;

namespace AutoMarket.Identity.Api.Auth;

// FR-AUTH-06 AC2–AC4. Token body-də (SEC-LOG-01). IP limiti bərpa sorğusu ilə ortaqdır (SEC-RATE-03, recovery-ip)
internal static class ResetPasswordEndpoint
{
    public static RouteGroupBuilder MapResetPassword(this RouteGroupBuilder group)
    {
        group.MapPost("/reset-password", HandleAsync)
            .WithName("ResetPassword")
            .AllowAnonymous()
            .RequireRateLimiting(RateLimitPolicies.Recovery)
            .AddEndpointFilter<ValidationFilter<ResetPasswordRequest>>()
            .Accepts<ResetPasswordRequest>("application/json")
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status429TooManyRequests);

        return group;
    }

    private static async Task<Results<NoContent, ApiProblemResult>> HandleAsync(
        ResetPasswordRequest request,
        ICommandHandler<ResetPasswordCommand> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(request.ToCommand(), cancellationToken);
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblem();
    }
}

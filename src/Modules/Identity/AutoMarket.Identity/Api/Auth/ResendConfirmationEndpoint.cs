using AutoMarket.BuildingBlocks.Application;
using AutoMarket.BuildingBlocks.Web.Errors;
using AutoMarket.BuildingBlocks.Web.RateLimiting;
using AutoMarket.BuildingBlocks.Web.Validation;
using AutoMarket.Identity.Application.Auth.ResendConfirmation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;

namespace AutoMarket.Identity.Api.Auth;

// FR-AUTH-02 AC3. SEC-RATE-03: IP limiti middleware-də, email limiti handler-də
internal static class ResendConfirmationEndpoint
{
    public static RouteGroupBuilder MapResendConfirmation(this RouteGroupBuilder group)
    {
        group.MapPost("/resend-confirmation", HandleAsync)
            .WithName("ResendConfirmation")
            .AllowAnonymous()
            .RequireRateLimiting(RateLimitPolicies.Recovery)
            .AddEndpointFilter<ValidationFilter<ResendConfirmationRequest>>()
            .Accepts<ResendConfirmationRequest>("application/json")
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status429TooManyRequests);

        return group;
    }

    private static async Task<Results<Accepted, ApiProblemResult>> HandleAsync(
        ResendConfirmationRequest request,
        ICommandHandler<ResendConfirmationCommand> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new ResendConfirmationCommand(request.Email!), cancellationToken);
        return result.IsSuccess ? TypedResults.Accepted((string?)null) : result.ToProblem();
    }
}

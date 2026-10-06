using AutoMarket.BuildingBlocks.Application;
using AutoMarket.BuildingBlocks.Web.Correlation;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;

namespace AutoMarket.BuildingBlocks.Web.Errors;

// RFC 9457 + code, message, traceId (SEC-ERR-01, CONVENTIONS §6.4)
public static class ProblemDetailsSetup
{
    public const string CodeKey = "code";
    public const string MessageKey = "message";
    public const string TraceIdKey = "traceId";

    public static IServiceCollection AddApiProblemDetails(this IServiceCollection services)
    {
        services.AddProblemDetails(options => options.CustomizeProblemDetails = Customize);
        services.AddExceptionHandler<GlobalExceptionHandler>();

        return services;
    }

    public static ProblemDetails ToProblemDetails(this Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        var problem = new ProblemDetails { Status = error.HttpStatus };
        problem.Extensions[CodeKey] = error.Code;
        problem.Extensions[MessageKey] = error.Message;

        return problem;
    }

    private static void Customize(ProblemDetailsContext context)
    {
        var problem = context.ProblemDetails;

        // Default traceId (Activity id) correlation id ilə əvəz olunur ki, client log-dakı dəyəri görsün
        problem.Extensions[TraceIdKey] = context.HttpContext.GetCorrelationId();

        // StatusCodePages-in boş 401/403 cavabları. 404/405 üçün REQUIREMENTS 4.8-də ümumi kod yoxdur
        if (!problem.Extensions.ContainsKey(CodeKey))
        {
            var error = problem.Status switch
            {
                StatusCodes.Status401Unauthorized => CommonErrors.Unauthorized,
                StatusCodes.Status403Forbidden => CommonErrors.Forbidden,
                _ => null,
            };

            if (error is not null)
            {
                problem.Extensions[CodeKey] = error.Code;
                problem.Extensions[MessageKey] = error.Message;
            }
        }
    }
}

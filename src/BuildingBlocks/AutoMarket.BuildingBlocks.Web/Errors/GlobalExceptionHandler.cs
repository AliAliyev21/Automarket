using AutoMarket.BuildingBlocks.Application;
using AutoMarket.BuildingBlocks.Domain;
using AutoMarket.BuildingBlocks.Web.Correlation;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace AutoMarket.BuildingBlocks.Web.Errors;

// SEC-ERR-01/02: exception-lar ProblemDetails + code-a çevrilir, detallar yalnız log-a yazılır (ARCHITECTURE §8.1)
internal sealed partial class GlobalExceptionHandler(
    IProblemDetailsService problemDetailsService,
    ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        var error = exception switch
        {
            DomainException domainException => ToError(domainException.Error),
            BadHttpRequestException { StatusCode: StatusCodes.Status413PayloadTooLarge } => CommonErrors.PayloadTooLarge,
            BadHttpRequestException { StatusCode: StatusCodes.Status415UnsupportedMediaType } => CommonErrors.UnsupportedMediaType,
            BadHttpRequestException => CommonErrors.ValidationFailed,
            _ => CommonErrors.InternalError,
        };

        // Exception handler correlation middleware-dən kənarda işləyir, ona görə scope burada yenidən açılır
        using (logger.BeginScope(new Dictionary<string, object> { ["CorrelationId"] = httpContext.GetCorrelationId() }))
        {
            if (error == CommonErrors.InternalError)
            {
                LogUnhandledException(logger, exception);
            }
            else
            {
                LogBadRequest(logger, exception, error.Code);
            }
        }

        httpContext.Response.StatusCode = error.HttpStatus;

        return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            Exception = exception,
            ProblemDetails = error.ToProblemDetails(),
        });
    }

    // CONVENTIONS §4.4: domen xətasının növü → HTTP status xəritəsi yalnız burada
    private static Error ToError(DomainError domainError) => new(
        domainError.Code,
        domainError.Message,
        domainError.Kind == DomainErrorKind.Validation ? StatusCodes.Status400BadRequest : StatusCodes.Status409Conflict);

    [LoggerMessage(Level = LogLevel.Error, Message = "Unhandled exception")]
    private static partial void LogUnhandledException(ILogger logger, Exception exception);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Bad request rejected with {ErrorCode}")]
    private static partial void LogBadRequest(ILogger logger, Exception exception, string errorCode);
}

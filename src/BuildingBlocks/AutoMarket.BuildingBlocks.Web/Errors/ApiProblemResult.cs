using System.Globalization;
using AutoMarket.BuildingBlocks.Application;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;

namespace AutoMarket.BuildingBlocks.Web.Errors;

// ProblemDetails + code cavabı (SEC-ERR-01). IProblemDetailsService ilə yazılır ki, traceId əlavə olunsun;
// RATE_LIMITED üçün Retry-After header-i də qoyulur (SEC-RATE)
public sealed class ApiProblemResult : IResult, IStatusCodeHttpResult
{
    private readonly ProblemDetails _problem;
    private readonly TimeSpan? _retryAfter;

    internal ApiProblemResult(ProblemDetails problem, TimeSpan? retryAfter = null)
    {
        _problem = problem;
        _retryAfter = retryAfter;
    }

    public int? StatusCode => _problem.Status;

    public async Task ExecuteAsync(HttpContext httpContext)
    {
        ArgumentNullException.ThrowIfNull(httpContext);

        httpContext.Response.StatusCode = _problem.Status ?? StatusCodes.Status500InternalServerError;
        if (_retryAfter is { } retryAfter)
        {
            httpContext.Response.Headers.RetryAfter = RetryAfterSeconds(retryAfter);
        }

        var problemDetailsService = httpContext.RequestServices.GetRequiredService<IProblemDetailsService>();
        await problemDetailsService.WriteAsync(new ProblemDetailsContext { HttpContext = httpContext, ProblemDetails = _problem });
    }

    public static string RetryAfterSeconds(TimeSpan retryAfter) =>
        Math.Max(1, (int)Math.Ceiling(retryAfter.TotalSeconds)).ToString(CultureInfo.InvariantCulture);
}

public static class ResultExtensions
{
    // Endpoint ProblemDetails-i əl ilə qurmur, yalnız ToProblem() istifadə edir (CONVENTIONS §6.4)
    public static ApiProblemResult ToProblem(this Result result)
    {
        ArgumentNullException.ThrowIfNull(result);

        var error = result.Error ?? throw new InvalidOperationException("A successful result cannot be converted to a problem.");
        return error.ToProblem();
    }

    public static ApiProblemResult ToProblem(this Error error)
    {
        ArgumentNullException.ThrowIfNull(error);
        return new ApiProblemResult(error.ToProblemDetails(), error.RetryAfter);
    }
}

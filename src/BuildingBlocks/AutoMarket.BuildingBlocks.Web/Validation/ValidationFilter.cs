using System.Text.Json;
using AutoMarket.BuildingBlocks.Application;
using AutoMarket.BuildingBlocks.Web.Errors;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Http;

namespace AutoMarket.BuildingBlocks.Web.Validation;

// SEC-INP-01: xəta olduqda handler-ə çatmadan 400 VALIDATION_FAILED + errors { field: [{ code, message }] } (ARCHITECTURE §8.2)
public sealed class ValidationFilter<TRequest>(IValidator<TRequest> validator) : IEndpointFilter
    where TRequest : class
{
    public const string ErrorsKey = ProblemDetailsSetup.ErrorsKey;

    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(next);

        var index = FindRequestIndex(context);
        if (index < 0 || context.Arguments[index] is not TRequest request)
        {
            return CommonErrors.ValidationFailed.ToProblem();
        }

        if (request is ISanitizableRequest<TRequest> sanitizable)
        {
            request = sanitizable.Sanitize();
            context.Arguments[index] = request;
        }

        var result = await validator.ValidateAsync(request, context.HttpContext.RequestAborted);
        if (!result.IsValid)
        {
            return ToProblem(result.Errors);
        }

        return await next(context);
    }

    private static int FindRequestIndex(EndpointFilterInvocationContext context)
    {
        for (var i = 0; i < context.Arguments.Count; i++)
        {
            if (context.Arguments[i] is TRequest)
            {
                return i;
            }
        }

        return -1;
    }

    private static ApiProblemResult ToProblem(IEnumerable<ValidationFailure> failures)
    {
        var errors = failures
            .GroupBy(failure => ToFieldName(failure.PropertyName), StringComparer.Ordinal)
            .ToDictionary(
                group => group.Key,
                group => group.Select(failure => new FieldError(failure.ErrorCode, failure.ErrorMessage)).ToArray(),
                StringComparer.Ordinal);

        var problem = CommonErrors.ValidationFailed.ToProblemDetails();
        problem.Extensions[ErrorsKey] = errors;

        return new ApiProblemResult(problem);
    }

    // JSON sahə adları camelCase-dir (CONVENTIONS §2.5): "Address.City" → "address.city"
    private static string ToFieldName(string propertyName) =>
        string.Join('.', propertyName.Split('.').Select(JsonNamingPolicy.CamelCase.ConvertName));

    private sealed record FieldError(string Code, string Message);
}

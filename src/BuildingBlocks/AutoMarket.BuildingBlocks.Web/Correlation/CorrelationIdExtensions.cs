using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace AutoMarket.BuildingBlocks.Web.Correlation;

public static class CorrelationIdExtensions
{
    public static IApplicationBuilder UseCorrelationId(this IApplicationBuilder app) =>
        app.UseMiddleware<CorrelationIdMiddleware>();

    // Middleware-dən əvvəl (və ya onsuz) çağırılanda ASP.NET Core-un TraceIdentifier-i qaytarılır
    public static string GetCorrelationId(this HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return context.Items.TryGetValue(CorrelationIdFormat.ItemKey, out var value) && value is string correlationId
            ? correlationId
            : context.TraceIdentifier;
    }
}

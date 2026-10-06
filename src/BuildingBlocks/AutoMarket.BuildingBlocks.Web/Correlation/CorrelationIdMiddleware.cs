using System.Diagnostics;
using AutoMarket.BuildingBlocks.Application;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace AutoMarket.BuildingBlocks.Web.Correlation;

// NFR-CORR: client-in X-Correlation-Id-si formatı ödəyirsə qəbul edilir, əks halda trace id istifadə olunur (ARCHITECTURE §8.4)
public sealed class CorrelationIdMiddleware(RequestDelegate next, ILogger<CorrelationIdMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var correlationId = CorrelationIdFormat.IsValid(context.Request.Headers[CorrelationIdFormat.HeaderName])
            ? context.Request.Headers[CorrelationIdFormat.HeaderName].ToString()
            : CreateCorrelationId();

        context.Items[CorrelationIdFormat.ItemKey] = correlationId;

        // Outbox envelope-u və audit qeydi correlation id-ni buradan oxuyur (ARCHITECTURE §8.4)
        CorrelationContext.Current = correlationId;

        // Header OnStarting-də yazılır: exception handler cavabı təmizləsə də header qalır
        context.Response.OnStarting(() =>
        {
            context.Response.Headers[CorrelationIdFormat.HeaderName] = correlationId;
            return Task.CompletedTask;
        });

        using (logger.BeginScope(new Dictionary<string, object> { ["CorrelationId"] = correlationId }))
        {
            await next(context);
        }
    }

    // W3C trace id ilə eyni dəyər log-ları və trace-ləri birləşdirir; Activity yoxdursa yeni UUID
    private static string CreateCorrelationId() =>
        Activity.Current is { } activity
            ? activity.TraceId.ToHexString()
            : Guid.CreateVersion7().ToString("N");
}

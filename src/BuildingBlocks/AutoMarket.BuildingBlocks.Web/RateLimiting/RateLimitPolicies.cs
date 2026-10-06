using System.Threading.RateLimiting;
using AutoMarket.BuildingBlocks.Application;
using AutoMarket.BuildingBlocks.Web.Errors;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AutoMarket.BuildingBlocks.Web.RateLimiting;

// Endpoint-lərdə RequireRateLimiting(RateLimitPolicies.X) ilə istifadə olunan adlı policy-lər (ARCHITECTURE §7.4).
// Body-dən gələn açarlar (email) və token sahibi üzrə limitlər handler-də IRateLimitService ilə yoxlanılır
public static partial class RateLimitPolicies
{
    // login-ip + login concurrency
    public const string Login = "login";

    // register (IP)
    public const string Register = "register";

    // recovery-ip (IP)
    public const string Recovery = "recovery";

    private const string UnknownIp = "unknown";

    public static IServiceCollection AddAutoMarketRateLimiting(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<RateLimitingOptions>()
            .Bind(configuration.GetSection(RateLimitingOptions.SectionName))
            .ValidateOnStart();
        services.AddSingleton<IValidateOptions<RateLimitingOptions>, RateLimitingOptionsValidator>();
        services.AddSingleton<IRateLimitService, RedisSlidingWindowRateLimitService>();

        services.AddSingleton(serviceProvider =>
        {
            var settings = serviceProvider.GetRequiredService<IOptions<RateLimitingOptions>>().Value.LoginConcurrency;
            return new LoginConcurrencyLimiter(new ConcurrencyLimiter(new ConcurrencyLimiterOptions
            {
                PermitLimit = settings.EffectivePermitLimit,
                QueueLimit = settings.QueueLimit,
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
            }));
        });

        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.OnRejected = OnRejectedAsync;

            options.AddPolicy(Login, context =>
            {
                var requestServices = context.RequestServices;
                var shared = requestServices.GetRequiredService<LoginConcurrencyLimiter>().Limiter;
                var rateLimitService = requestServices.GetRequiredService<IRateLimitService>();
                return RateLimitPartition.Get(GetIp(context), ip =>
                    new CompositeRateLimiter(new DistributedRateLimiter(rateLimitService, RateLimitRules.LoginIp, ip), shared));
            });

            options.AddPolicy(Register, context => IpPartition(context, RateLimitRules.Register));
            options.AddPolicy(Recovery, context => IpPartition(context, RateLimitRules.RecoveryIp));
        });

        return services;
    }

    // SEC-RATE-12: IP yalnız ForwardedHeaders middleware-inin etibarlı proxy-lərdən bərpa etdiyi RemoteIpAddress-dir
    private static string GetIp(HttpContext context) => context.Connection.RemoteIpAddress?.ToString() ?? UnknownIp;

    private static RateLimitPartition<string> IpPartition(HttpContext context, string rule)
    {
        var rateLimitService = context.RequestServices.GetRequiredService<IRateLimitService>();
        return RateLimitPartition.Get(GetIp(context), ip => new DistributedRateLimiter(rateLimitService, rule, ip));
    }

    // 429 + RATE_LIMITED ProblemDetails + Retry-After (SEC-RATE). Aqreqasiya edilmiş audit (SEC-RATE-13) sonrakı mərhələdədir
    private static async ValueTask OnRejectedAsync(OnRejectedContext context, CancellationToken cancellationToken)
    {
        var httpContext = context.HttpContext;
        var retryAfter = context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var value) ? value : (TimeSpan?)null;

        var logger = httpContext.RequestServices.GetRequiredService<ILoggerFactory>().CreateLogger(typeof(RateLimitPolicies));
        LogRejected(logger, (httpContext.GetEndpoint() as RouteEndpoint)?.RoutePattern.RawText);

        var error = retryAfter is { } delay ? CommonErrors.RateLimitedFor(delay) : CommonErrors.RateLimited;
        await error.ToProblem().ExecuteAsync(httpContext);
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "Rate limit exceeded on {RoutePattern}")]
    private static partial void LogRejected(ILogger logger, string? routePattern);
}

// Bütün login sorğuları üçün bir ConcurrencyLimiter (singleton)
internal sealed class LoginConcurrencyLimiter(RateLimiter limiter) : IDisposable
{
    public RateLimiter Limiter { get; } = limiter;

    public void Dispose() => Limiter.Dispose();
}

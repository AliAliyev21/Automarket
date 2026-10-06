using System.Globalization;
using System.Reflection;
using Serilog;
using Serilog.Events;
using Serilog.Formatting.Compact;

namespace AutoMarket.Api.Logging;

// ADR-0013, ARCHITECTURE §8.3: kod yalnız ILogger<T> istifadə edir, Serilog yalnız Host-da provider kimi qoşulur
internal static class SerilogSetup
{
    private const string RequestLogTemplate = "HTTP {RequestMethod} {RoutePattern} responded {StatusCode} in {Elapsed:0.0000} ms";

    public static WebApplicationBuilder AddAutoMarketLogging(this WebApplicationBuilder builder)
    {
        var environment = builder.Environment;
        var configuration = builder.Configuration;
        var version = typeof(SerilogSetup).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
            ?? "unknown";

        // preserveStaticLogger: statik Log.Logger dəyişdirilmir, testlərdə bir neçə host paralel işləyə bilir
        builder.Services.AddSerilog(
            (_, logger) =>
            {
                logger
                    .ReadFrom.Configuration(configuration)
                    .Enrich.FromLogContext()
                    .Enrich.WithProperty("Application", environment.ApplicationName)
                    .Enrich.WithProperty("Environment", environment.EnvironmentName)
                    .Enrich.WithProperty("Version", version)
                    .WriteTo.Console(new CompactJsonFormatter());

                // Seq yalnız lokal mühitdə (ADR-0013, ARCHITECTURE §13 A8)
                var seqServerUrl = configuration["Seq:ServerUrl"];
                if (environment.IsDevelopment() && !string.IsNullOrWhiteSpace(seqServerUrl))
                {
                    logger.WriteTo.Seq(seqServerUrl, formatProvider: CultureInfo.InvariantCulture);
                }
            },
            preserveStaticLogger: true);

        return builder;
    }

    // NFR-LOG: hər sorğu üçün bir qeyd; RequestPath əvəzinə route şablonu, query string yazılmır
    public static IApplicationBuilder UseAutoMarketRequestLogging(this IApplicationBuilder app) =>
        app.UseSerilogRequestLogging(options =>
        {
            // Statik Log.Logger saxlanılmır (preserveStaticLogger), ona görə DI-dakı logger açıq verilir
            options.Logger = app.ApplicationServices.GetRequiredService<Serilog.ILogger>();
            options.MessageTemplate = RequestLogTemplate;
            options.GetLevel = GetLevel;
            options.GetMessageTemplateProperties = (context, _, elapsed, statusCode) =>
            [
                new LogEventProperty("RequestMethod", new ScalarValue(context.Request.Method)),
                new LogEventProperty("RoutePattern", new ScalarValue(GetRoutePattern(context))),
                new LogEventProperty("StatusCode", new ScalarValue(statusCode)),
                new LogEventProperty("Elapsed", new ScalarValue(elapsed)),
            ];
        });

    // Health sorğuları production log-unu doldurmasın deyə Verbose (ARCHITECTURE §8.5)
    private static LogEventLevel GetLevel(HttpContext context, double elapsed, Exception? exception)
    {
        if (context.Request.Path.StartsWithSegments("/health", StringComparison.OrdinalIgnoreCase))
        {
            return LogEventLevel.Verbose;
        }

        return exception is not null || context.Response.StatusCode >= StatusCodes.Status500InternalServerError
            ? LogEventLevel.Error
            : LogEventLevel.Information;
    }

    private static string GetRoutePattern(HttpContext context) =>
        (context.GetEndpoint() as RouteEndpoint)?.RoutePattern.RawText ?? "(unmatched)";
}

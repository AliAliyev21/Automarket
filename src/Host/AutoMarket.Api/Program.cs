using System.Text.Json.Serialization;
using AutoMarket.Api.Configuration;
using AutoMarket.Api.HealthChecks;
using AutoMarket.Api.Logging;
using AutoMarket.BuildingBlocks.Application;
using AutoMarket.BuildingBlocks.Audit;
using AutoMarket.BuildingBlocks.Messaging;
using AutoMarket.BuildingBlocks.Persistence;
using AutoMarket.BuildingBlocks.Platform;
using AutoMarket.BuildingBlocks.Security;
using AutoMarket.BuildingBlocks.Web;
using AutoMarket.BuildingBlocks.Web.Correlation;
using AutoMarket.BuildingBlocks.Web.Errors;
using AutoMarket.BuildingBlocks.Web.RateLimiting;
using AutoMarket.BuildingBlocks.Web.Security;
using AutoMarket.Catalog;
using AutoMarket.Engagement;
using AutoMarket.Identity;
using AutoMarket.Listings;
using AutoMarket.Messaging;
using AutoMarket.Moderation;
using AutoMarket.Notifications;
using AutoMarket.Search;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// ARCHITECTURE §8.6: AutoMarket__Postgres__ConnectionString formatlı environment variable-lar
builder.Configuration.AddEnvironmentVariables(prefix: "AutoMarket__");

builder.AddAutoMarketLogging();

// SEC-INP-02: JSON iç-içəlik dərinliyi məhdudlaşdırılır, bilinməyən sahələr nəzərə alınmır (default), enum-lar string (ARCHITECTURE §7.2)
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.MaxDepth = 32;
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
});

builder.Services
    .AddApiProblemDetails()
    .AddOpenApi()
    .AddInfrastructureClients(builder.Configuration)
    .AddDependencyHealthChecks();

builder.Services
    .AddBuildingBlocksApplication()
    .AddBuildingBlocksSecurity()
    .AddBuildingBlocksWeb(builder.Configuration)
    .AddMessaging(builder.Configuration)
    .AddPlatform()
    .AddAudit()
    .AddTrustedForwardedHeaders(builder.Configuration)
    .AddAutoMarketCors()
    .AddAutoMarketAuthorization()
    .AddAutoMarketRateLimiting(builder.Configuration);

builder.Services
    .AddIdentityModule(builder.Configuration)
    .AddCatalogModule(builder.Configuration)
    .AddListingsModule(builder.Configuration)
    .AddSearchModule(builder.Configuration)
    .AddModerationModule(builder.Configuration)
    .AddEngagementModule(builder.Configuration)
    .AddMessagingModule(builder.Configuration)
    .AddNotificationsModule(builder.Configuration);

var app = builder.Build();

// ARCHITECTURE §4.4: migration-lar yalnız Development-də startup-da tətbiq olunur, digər mühitlərdə deploy addımıdır
if (app.Environment.IsDevelopment() && app.Configuration.GetValue("Database:MigrateOnStartup", defaultValue: true))
{
    foreach (var migrator in app.Services.GetServices<IModuleMigrator>())
    {
        await migrator.MigrateAsync(CancellationToken.None);
    }
}

// Middleware sırası ARCHITECTURE §7.1-ə uyğundur. HSTS/HTTPS redirection və security header-lər sonrakı mərhələdədir
app.UseForwardedHeaders();
app.UseExceptionHandler();
app.UseStatusCodePages();
app.UseCorrelationId();
app.UseAutoMarketRequestLogging();
app.UseRouting();
app.UseCors();
app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();

// NFR-DOC: interaktiv UI production-da açıq deyil (ARCHITECTURE §8.8)
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi().AllowAnonymous();
    app.MapScalarApiReference().AllowAnonymous();
}

app.MapHealthEndpoints(app.Environment);

app.MapIdentityEndpoints();
app.MapCatalogEndpoints();
app.MapListingsEndpoints();
app.MapSearchEndpoints();
app.MapModerationEndpoints();
app.MapEngagementEndpoints();
app.MapMessagingEndpoints();
app.MapNotificationsEndpoints();

// Prosesin işlədiyini sadə yoxlamaq üçün
app.MapGet("/ping", () => TypedResults.Ok("pong"))
    .WithName("Ping")
    .AllowAnonymous();

await app.RunAsync();

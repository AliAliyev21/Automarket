using AutoMarket.Api.Configuration;
using AutoMarket.Api.HealthChecks;
using AutoMarket.Api.Logging;
using AutoMarket.BuildingBlocks.Web.Correlation;
using AutoMarket.BuildingBlocks.Web.Errors;
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

builder.Services
    .AddApiProblemDetails()
    .AddOpenApi()
    .AddInfrastructureClients(builder.Configuration)
    .AddDependencyHealthChecks();

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

// Middleware sırası ARCHITECTURE §7.1-ə uyğundur; qalan addımlar (ForwardedHeaders, HSTS, security header-lər,
// CORS, auth, rate limit) aid mərhələlərdə əlavə olunur
app.UseExceptionHandler();
app.UseStatusCodePages();
app.UseCorrelationId();
app.UseAutoMarketRequestLogging();
app.UseRouting();

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
